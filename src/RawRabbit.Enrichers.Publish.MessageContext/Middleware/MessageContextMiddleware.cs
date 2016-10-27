﻿using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Publish;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Publish.MessageContext.Middleware
{
	public class MessageContextMiddleware<TMessageContext> : StagedMiddleware where TMessageContext : IMessageContext, new()
	{
		private readonly JsonSerializer _serializer;

		public MessageContextMiddleware(JsonSerializer serializer)
		{
			_serializer = serializer;
		}
		public override Task InvokeAsync(IPipeContext context)
		{
			var globalMessageId = context.GetGlobalMessageId();
			if (globalMessageId == Guid.Empty)
			{
				globalMessageId = Guid.NewGuid();
			}
			var messageContext = new TMessageContext
			{
				GlobalRequestId = globalMessageId
			};
			context.Properties.Add(PipeKey.MessageContext, messageContext);

			var properties = context.GetBasicProperties();
			string serializedProps;
			using (var sw = new StringWriter())
			{
				_serializer.Serialize(sw, messageContext);
				serializedProps = sw.GetStringBuilder().ToString();
			}

			properties.Headers.Add(PropertyHeaders.Context, serializedProps);
			return Next.InvokeAsync(context);
		}

		public override string StageMarker => PublishStage.BasicPropertiesCreated.ToString();
	}
}
