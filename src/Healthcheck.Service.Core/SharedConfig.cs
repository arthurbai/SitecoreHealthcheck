﻿namespace Healthcheck.Service.Core
{
    using Sitecore.Configuration;
    using System;
    using System.Configuration;

    public static class SharedConfig
    {
        public static string ConnectionStringOrKey
        {
            get
            {
                var connectionStringOrKey = Settings.GetSetting("Healthcheck.ConnectionStringKeyOrConnectionString");

                if (connectionStringOrKey.StartsWith("Endpoint=sb://", StringComparison.OrdinalIgnoreCase))
                {
                    return connectionStringOrKey;
                }
                else
                {
                    return ConfigurationManager.ConnectionStrings[connectionStringOrKey].ConnectionString;
                }
            }
        }

        public static string TopicName => Settings.GetSetting("Healthcheck.TopicName");

        public static string IncomingQueueName => Settings.GetSetting("Healthcheck.IncomingQueueName");

        public static string SubscriptionName
        {
            get
            {
                if (string.IsNullOrEmpty(Settings.GetSetting("Healthcheck.SubscritionName")))
                {
                    return Settings.InstanceName;
                }
                else
                {
                    return Settings.GetSetting("Healthcheck.SubscritionName");
                }
            }
        }

        public static string InstanceName
        {
            get
            {
                if (string.IsNullOrEmpty(Settings.GetSetting("Healthcheck.Instancename")))
                {
                    return Settings.InstanceName;
                }
                else
                {
                    return Settings.GetSetting("Healthcheck.Instancename");
                }
            }
        }
    }
}