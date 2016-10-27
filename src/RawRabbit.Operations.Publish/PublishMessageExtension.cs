﻿using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Publish;
using RawRabbit.Context;
using RawRabbit.Operations.Publish;
using RawRabbit.Operations.Publish.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class PublishMessageExtension
	{
		private static readonly Action<IPipeBuilder> PublishPipeAction = pipe => pipe
			.Use<PublishConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.ExchangeConfigured))
			.Use<ExchangeDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.ExchangeDeclared))
			.Use<Operations.Publish.Middleware.RoutingKeyMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.RoutingKeyCreated))
			.Use<Operations.Publish.Middleware.MessageSerializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.MessageSerialized))
			.Use<Operations.Publish.Middleware.BasicPropertiesMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.BasicPropertiesCreated))
			.Use<PublishChannelMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.ChannelCreated))
			.Use<MandatoryCallbackMiddleware>()
			.Use<PublishAcknowledgeMiddleware>()
			.Use<PublishMessage>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.MessagePublished));

		public static Task PublishAsync<TMessage>(this IBusClient client, TMessage message, Action<IPublishConfigurationBuilder> config = null)
		{
			return client.InvokeAsync(
				PublishPipeAction,
				ctx =>
				{
					ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					ctx.Properties.Add(PipeKey.Message, message);
					ctx.Properties.Add(PipeKey.ConfigurationAction, config);
				});
		}
	}
}
