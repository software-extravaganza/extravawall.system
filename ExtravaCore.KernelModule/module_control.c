#include "module_control.h"

extern int default_packet_response;
module_param(default_packet_response, int, 0644);
MODULE_PARM_DESC(default_packet_response, "Default packet response (when user space isn't connected): 1=DROP, 2=ACCEPT; default=1");

extern bool force_icmp;
module_param(force_icmp, bool, 0644);
MODULE_PARM_DESC(force_icmp, "Forces only ICMP packet filtering (for testing): 0=ALL PACKETS, 1=ICMP PACKETS ONLY; default=0");


static bool _shouldCapture = false;
static bool _isInitialized = false;
static bool _isActive = false;
static bool _isUnloading = false;
static bool _isUserSpaceReadConnected = false;
static bool _isUserSpaceWriteConnected = false;
static bool _isUserSpaceConnected = false;
static int _logLevel = 1;
static bool _oldShouldCapture = true;
static bool _oldIsActive = true;
static bool _oldIsInitialized = true;
static bool _oldIsUserSpaceReadConnected = true;
static bool _oldIsUserSpaceWriteConnected = true;
static bool _oldIsUserSpaceConnected = true;
static int _oldLogLevel = -9999;
static captureEventHandler _shouldCaptureUserEventHandler = NULL;

static void _shouldCaptureChangeHandler(void);
static void _userSpaceConnectionChangeHandler(void);

bool IsActive(void){
    return _isActive;
}

bool IsUnloading(void){
    return _isUnloading;
}


bool IsInitialized(void){
    return _isInitialized;
}

bool IsUserSpaceConnected(void){
    return _isUserSpaceConnected;
}

bool IsUserSpaceReadConnected(void){
    return _isUserSpaceReadConnected;
}

bool IsUserSpaceWriteConnected(void){
    return _isUserSpaceWriteConnected;
}

bool ShouldCapture(void){
    return _shouldCapture;
}

void Activate(void){
    if(_isActive == true){
        return;
    }

    _oldIsActive = _isActive;
    _isActive = true;
    _shouldCaptureChangeHandler();
}

void Deactivate(void){
    if(_isActive == false){
        return;
    }

    _oldIsActive = _isActive;
    _isActive = false;
    _shouldCaptureChangeHandler();
}

void SetInitialized(void){
    if(_isInitialized == true){
        return;
    }

    _oldIsInitialized = _isInitialized;
    _isInitialized = true;
    _shouldCaptureChangeHandler();
}

void SetUnloading(void){
    _isUnloading = true;
    _shouldCaptureChangeHandler();
}

void SetUserSpaceReadConnected(void){
    if(_isUserSpaceReadConnected == true){
        return;
    }

    _oldIsUserSpaceReadConnected = _isUserSpaceReadConnected;
    _isUserSpaceReadConnected = true;
    _userSpaceConnectionChangeHandler();
}

void SetUserSpaceWriteConnected(void){
    if(_isUserSpaceWriteConnected == true){
        return;
    }

    _oldIsUserSpaceWriteConnected = _isUserSpaceWriteConnected;
    _isUserSpaceWriteConnected = true;
    _userSpaceConnectionChangeHandler();
}

void SetUninitialized(void){
    if(_isInitialized == false){
        return;
    }

    _oldIsInitialized = _isInitialized;
    _isInitialized = false;
    _shouldCaptureChangeHandler();
}

void SetUserSpaceReadDisconnected(void){
    if(_isUserSpaceReadConnected == false){
        return;
    }

    _oldIsUserSpaceReadConnected = _isUserSpaceReadConnected;
    _isUserSpaceReadConnected = false;
    _userSpaceConnectionChangeHandler();
}

void SetUserSpaceWriteDisconnected(void){
    if(_isUserSpaceWriteConnected == false){
        return;
    }

    _oldIsUserSpaceWriteConnected = _isUserSpaceWriteConnected;
    _isUserSpaceWriteConnected = false;
    _userSpaceConnectionChangeHandler();
}

void SetLogLevel(int level){
    if(_oldLogLevel == level){
        return;
    }

    _oldLogLevel = _logLevel;
    _logLevel = level;
    __LoggerSetLevel(_logLevel);
    LOG_INFO("📜 Log level changed to %d", level);
}

int GetLogLevel(void){
    return _logLevel;
}

void SetShouldCaptureEventHandler(captureEventHandler handler){
    _shouldCaptureUserEventHandler = handler;
    if(_shouldCaptureUserEventHandler != NULL){
        _shouldCaptureUserEventHandler(_shouldCapture);
    }
}

static void _shouldCaptureChangeHandler(void){
    bool _newShouldCapture = IsActive() && IsInitialized() && IsUserSpaceConnected() && !IsUnloading();
    if(_oldShouldCapture == _newShouldCapture){
        return;
    }

    _oldShouldCapture = _shouldCapture;
    _shouldCapture = _newShouldCapture;

    if(_shouldCaptureUserEventHandler != NULL){
        _shouldCaptureUserEventHandler(_shouldCapture);
    }

    char* captureState = _shouldCapture ? "🟢" : "🔴";
    char* captureStateText = _shouldCapture ? "online" : "offline";
    LOG_INFO("%s Packet capture is %s", captureState, captureStateText);
}

static void _userSpaceConnectionChangeHandler(){
    _oldIsUserSpaceConnected = _isUserSpaceConnected;
    _isUserSpaceConnected = IsUserSpaceReadConnected() && IsUserSpaceWriteConnected();
    char* readState = _isUserSpaceReadConnected ? "📤" : "🗙";
    char* writeState = _isUserSpaceWriteConnected ? "📥" : "🗙";
    char* readStateText = _isUserSpaceReadConnected ? "online" : "offline";
    char* writeStateText = _isUserSpaceWriteConnected ? "online" : "offline";
    char* connectedState = _isUserSpaceConnected ? "🟢" : "🔴";
    char* connectedStateText = _isUserSpaceConnected ? "connected" : "disconnected";
    LOG_INFO("%s %s%s User space is %s (Read:%s / Write:%s)", connectedState, readState, writeState, connectedStateText, readStateText, writeStateText);
    _shouldCaptureChangeHandler();
}