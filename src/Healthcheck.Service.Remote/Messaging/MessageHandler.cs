﻿using Healthcheck.Service.Core;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Healthcheck.Service.Remote.Messaging
{
    public class MessageHandler
    {
        public static async Task ReceiveMessage(Message message, CancellationToken token)
        {
            // TODO, PROCESS and RUN Healthcheck!
            Sitecore.Diagnostics.Log.Info(Encoding.UTF8.GetString(message.Body), "MessageHandler");

            //TEST, REGISTER IT
            var sender = new MessageSender(SharedConfig.ConnectionStringOrKey, SharedConfig.IncomingQueueName);

            await sender.SendAsync(new Message
            {
                Body = Encoding.UTF8.GetBytes($"SENT BACK FROM{SharedConfig.SubscriptionName}")
            }).ConfigureAwait(false);

        }

        public static Task LogMessageHandlerException(ExceptionReceivedEventArgs e)
        {
            Sitecore.Diagnostics.Log.Info(string.Format("Exception: \"{0}\" {1}", e.Exception.Message, e.ExceptionReceivedContext.EntityPath), "MessageHandler");
            return Task.CompletedTask;
        }
    }
}
