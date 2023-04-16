class Screen {
    public string Name;
    private float amps;
    public string Content { get; set; } = "Welcome to the ATM";

    public void WriteToDisplay(string stuffToWrite) {
        Content = stuffToWrite;

        string blah = Content;
    }
}

class Input {
    public string Name;
    string LastEnteredValue;
}

class Keypad : Input {
    public string Name;
    string[] Buttons;
}


class Motor {
    public string Name;
    public float voltage;
    public void Start() {
        voltage = 12;

        Atm.Bob myFriend = new Atm.Bob();
    }
}

class Atm {
    Screen MyScreen = new Screen();
    Keypad MyKeypad = new Keypad();
    Motor Motor1 = new Motor();
    Motor Motor2 = new Motor();
    Motor Motor3 = new Motor();
    public void StartUpATtmMachine() {
        MyScreen.Name = "ATM Screen";
        MyScreen.Content = "Welcome";
        MyScreen.Content = "Enter your pin";
        MyKeypad.LastEnteredValue = "1234";

        Motor1.Name = "Money Intake";
        Motor2.Name = "Money Dispenser";
        Motor3.Name = "Vault Door";

        Motor1.Start();
        Motor1.voltage = 2;

        Bob myFriend = new Bob();
        myFriend.Name = "Alice";

        //MyScreen.WriteToDisplay(myFriend.Name);
        MyScreen.Content = "asdasdasd";
    }

    class Bob {
        string Name = "Bob";
    }

}
