﻿namespace Healthcheck.Service.Domain.Remote
{
    using Healthcheck.Service.Core;
    using Healthcheck.Service.Core.Messages;
    using Healthcheck.Service.Remote.EventQueue.Models.Event;
    using Microsoft.Azure.ServiceBus.Core;
    using Newtonsoft.Json;
    using Sitecore.Configuration;
    using Sitecore.Data.Items;
    using System;
    using System.Text;

    /// <summary>
    /// Remote Xconnect Api check component
    /// </summary>
    /// <seealso cref="Healthcheck.Service.Domain.RemoteBaseComponent" />
    public class RemoteXConnectApiCheck : RemoteBaseComponent
    {
        /// <summary>
        /// Gets or sets the x connect API connection string key.
        /// </summary>
        /// <value>
        /// The x connect API connection string key.
        /// </value>
        public string XConnectApiConnectionStringKey { get; set; }

        /// <summary>
        /// Gets or sets the x connect API certificate connection string key.
        /// </summary>
        /// <value>
        /// The x connect API certificate connection string key.
        /// </value>
        public string XConnectApiCertificateConnectionStringKey { get; set; }

        /// <summary>
        /// Gets or sets the warn before.
        /// </summary>
        /// <value>
        /// The warn before.
        /// </value>
        public int WarnBefore { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteXConnectApiCheck"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        public RemoteXConnectApiCheck(Item item) : base(item)
        {
            this.XConnectApiConnectionStringKey = item["XConnect Api ConnectionString Key"];
            this.XConnectApiCertificateConnectionStringKey = item["XConnect Certificate ConnectionString Key"];
            this.WarnBefore = Sitecore.MainUtil.GetInt(item["Warn Before"], 100);
        }

        /// <summary>
        /// Runs the healthcheck.
        /// </summary>
        public override void RunHealthcheck()
        {
            var dateTime = DateTime.UtcNow;
            this.SaveRemoteCheckStarted(dateTime);

            var message = new OutGoingMessage
            {
                Parameters = new System.Collections.Generic.Dictionary<string, string>
                {
                    {"XConnectApiConnectionStringKey", this.XConnectApiConnectionStringKey },
                    {"XConnectApiCertificateConnectionStringKey", this.XConnectApiCertificateConnectionStringKey },
                     {"Warn Before", this.WarnBefore.ToString() }
                },
                TargetInstance = this.TargetInstance,
                ComponentId = this.InnerItem.ID.Guid,
                EventRaised = dateTime
            };
            if (Settings.GetSetting("Healthcheck.Remote.Mode").Equals("eventqueue", StringComparison.OrdinalIgnoreCase))
            {
                var remoteEvent = new HealthcheckStartedRemoteEvent(Constants.TemplateNames.RemoteXConnectApiCheckTemplateName, "healthcheck:started:remote", message);
                var database = Sitecore.Configuration.Factory.GetDatabase("web");
                var eventQueue = database.RemoteEvents.EventQueue;
                eventQueue.QueueEvent<HealthcheckStartedRemoteEvent>(remoteEvent, true, false);
            }
            else
            {
                var busMessage = new Microsoft.Azure.ServiceBus.Message
                {
                    ContentType = "application/json",
                    Label = Constants.TemplateNames.RemoteXConnectApiCheckTemplateName,
                    Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message))
                };

                var messageSender = new MessageSender(SharedConfig.ConnectionStringOrKey, SharedConfig.TopicName);
                messageSender.SendAsync(busMessage).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
    }
}