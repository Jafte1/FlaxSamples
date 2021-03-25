using System;
using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.GUI;

namespace FirstPersonShooter
{
    public class TestUI1 : Script
    {
        public UIControl myUI;
        private Label myLabel;
        public float speed = 50f;
        public override void OnStart()
        {
            // Here you can add code that needs to be called when script is created, just before the first game update
            myLabel = myUI.Get<Label>();
        }

        public override void OnEnable()
        {
            // Here you can add code that needs to be called when script is enabled (eg. register for events)
        }

        public override void OnDisable()
        {
            // Here you can add code that needs to be called when script is disabled (eg. unregister from events)
        }

        public override void OnUpdate()
        {
            myLabel.Text = speed.ToString();
            // Here you can add code that needs to be called every frame
        }
    }
}
