﻿namespace Healthcheck.Service.Remote
{
    using Healthcheck.Service.Core;
    using Healthcheck.Service.Remote.Messaging;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Management;
    using Sitecore.DependencyInjection;
    using Sitecore.Pipelines;
    using System;

    public class InitializeSubscriptionClient
    {
        public virtual void Process(PipelineArgs args)
        {
            Sitecore.Diagnostics.Log.Info("Initialize Healthcheck Topic", this);
            try
            {
                ISubscriptionClient client = (ISubscriptionClient)ServiceLocator.ServiceProvider.GetService(typeof(ISubscriptionClient));

                var _managementClient = new ManagementClient(new ServiceBusConnectionStringBuilder(SharedConfig.ConnectionStringOrKey));
                EnsureTopicExists(_managementClient, SharedConfig.TopicName);
                EnsureSubscriptionExists(_managementClient, SharedConfig.TopicName, SharedConfig.SubscriptionName);

                client.RegisterMessageHandler(MessageHandler.ReceiveMessage,
                    new MessageHandlerOptions((e) => MessageHandler.LogMessageHandlerException(e)) { AutoComplete = true, MaxConcurrentCalls = 1 });
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error("Initialize failed Healthcheck Topic", ex, this);
            }
            Sitecore.Diagnostics.Log.Info("Initialize finished Healthcheck Topic", this);
        }

        private TopicDescription EnsureTopicExists(ManagementClient _managementClient, string topicName)
        {
            try
            {
                return _managementClient.GetTopicAsync(topicName).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                // it's OK... try and create it instead
            }

            try
            {
                return _managementClient.CreateTopicAsync(topicName).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                return _managementClient.GetTopicAsync(topicName).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Could not create topic '{topicName}'", exception);
            }
        }

        private SubscriptionDescription EnsureSubscriptionExists(ManagementClient _managementClient, string topicName, string subscription)
        {
            try
            {
                return _managementClient.GetSubscriptionAsync(topicName, subscription).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                // it's OK... try and create it instead
            }

            try
            {
                return _managementClient.CreateSubscriptionAsync(topicName, subscription).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                return _managementClient.GetSubscriptionAsync(topicName, subscription).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Could not create topic '{topicName}'", exception);
            }
        }
    }
}