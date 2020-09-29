﻿using Healthcheck.Service.Core;
using Healthcheck.Service.Core.Messages;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json;
using Sitecore.Configuration;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Healthcheck.Service.Remote.Messaging
{
    public class MessageHandler
    {
        public static async Task ReceiveMessage(Message message, CancellationToken token)
        {
            Sitecore.Diagnostics.Log.Info(Encoding.UTF8.GetString(message.Body), "MessageHandler");

            if (message.ContentType.Equals("application/json"))
            {
                HealthcheckResult result = null;
                var messageContract = JsonConvert.DeserializeObject<OutGoingMessage>(Encoding.UTF8.GetString(message.Body));

                if (!SharedConfig.SubscriptionName.Equals(messageContract.TargetInstance, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (message.Label.Equals(Constants.TemplateNames.RemoteLogFileCheckTemplateName))
                {
                    result = LogFileCheck.RunHealthcheck(messageContract.Parameters["FileNameFormat"], Path.Combine(Sitecore.Configuration.Settings.DataFolder, "logs"), DateTime.ParseExact(messageContract.Parameters["ItemCreationDate"], "yyyyMMddTHHmmss", CultureInfo.InvariantCulture), int.Parse(messageContract.Parameters["NumberOfDaysToCheck"]));
                }
                else if (message.Label.Equals(Constants.TemplateNames.RemoteCertificateCheckTemplateName))
                {
                    result = CertificateCheck.RunHealthcheck(messageContract.Parameters["StoreName"], messageContract.Parameters["Location"], messageContract.Parameters["Value"], messageContract.Parameters["FindByType"], int.Parse(messageContract.Parameters["Warn Before"]));
                }
                else if (message.Label.Equals(Constants.TemplateNames.RemoteXConnectApiCheckTemplateName))
                {
                    result = XConnectApiCheck.RunHealthcheck(messageContract.Parameters["XConnectApiCertificateConnectionStringKey"], messageContract.Parameters["XConnectApiConnectionStringKey"], int.Parse(messageContract.Parameters["Warn Before"]));
                }

                if (result != null)
                {
                    var sender = new MessageSender(SharedConfig.ConnectionStringOrKey, SharedConfig.IncomingQueueName);

                    var incomingMessage = new HealthcheckResultMessage
                    {
                        Result = result,
                        ComponentId = messageContract.ComponentId,
                        LastCheckTime = messageContract.EventRaised
                    };
                    await sender.SendAsync(new Message
                    {
                        Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(incomingMessage)),
                        ContentType = "application/json",
                        Label = "Result"
                    }).ConfigureAwait(false);                    
                }
            }
        }

        public static Task LogMessageHandlerException(ExceptionReceivedEventArgs e)
        {
            Sitecore.Diagnostics.Log.Info(string.Format("Exception: \"{0}\" {1}", e.Exception.Message, e.ExceptionReceivedContext.EntityPath), "MessageHandler");
            return Task.CompletedTask;
        }
    }
}
