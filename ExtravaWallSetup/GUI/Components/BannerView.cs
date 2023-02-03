﻿using Figgle;
//------------------------------------------------------------------------------

//  <auto-generated>
//      This code was generated by:
//        TerminalGuiDesigner v1.0.22.0
//      You can make changes to this file and they will not be overwritten when saving.
//  </auto-generated>
// -----------------------------------------------------------------------------
namespace ExtravaWallSetup.GUI {
    using NStack;
    using Terminal.Gui;
     public enum BannerType {
        Welcome,
        ExitSuccess,
        ExitFail
    }



    [GenerateFiggleText("ExtravaWall", "standard", "ExtravaWall")]
    [GenerateFiggleText("Version", "nancyj", "     v 0.1")]
    [GenerateFiggleText("ExitSuccess", "smslant", "go play in traffic")]
    [GenerateFiggleText("ExitFail", "thin", "Failed")]
    public partial class BannerView {
        private int _height;

        public ustring Text { get => bannerText.Text ?? string.Empty; }
        public int BannerHeight { get => _height; }
        public BannerView(BannerType bannerType) {
            InitializeComponent();
            (bannerText.Text, _height) = bannerType switch {
                BannerType.Welcome => ($"\n\n{ExtravaWall}{Version}", 15),
                BannerType.ExitSuccess => ($"\n{ExitSuccess}\n(network traffic)", 9),
                BannerType.ExitFail => ($"\n{ExitFail}\n¯\\_(⊙︿⊙)_/¯\n", 15),
                _ => (string.Empty, 1)
            };
            bannerText.Height = _height;
            Height = _height;


            CanFocus = false;
            bannerText.CanFocus = false;
            AutoSize = false;
            bannerText.AutoSize = false;
            Width = 70;
            
        }

  
  
    }

   
}
