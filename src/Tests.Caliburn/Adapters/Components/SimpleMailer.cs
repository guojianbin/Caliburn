﻿namespace Tests.Caliburn.Adapters.Components
{
    public class SimpleMailer : IMailer
    {
        public SimpleMailer(ILogger logger)
        {
            Logger = logger;
        }

        public ILogger Logger { get; private set; }

        public void SendMessage(string address, string message)
        {
            //send the message
        }
    }
}