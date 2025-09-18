using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Cognition.Jobs
{
    public class SignalRNotifier
    {
        private readonly string _hubUrl;
        private readonly HubConnection _connection;

        public SignalRNotifier(string hubUrl)
        {
            _hubUrl = hubUrl;
            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect()
                .Build();
        }

        public async Task StartAsync()
        {
            await _connection.StartAsync();
        }

        public async Task NotifyAssistantMessageAsync(Guid conversationId, Guid personaId, string content, Guid? messageId = null)
        {
            if (_connection.State != HubConnectionState.Connected)
            {
                await _connection.StartAsync();
            }
            if (messageId.HasValue)
            {
                await _connection.InvokeAsync("SendAssistantMessage", conversationId.ToString(), personaId.ToString(), content, messageId.Value.ToString());
            }
            else
            {
                await _connection.InvokeAsync("SendAssistantMessage", conversationId.ToString(), personaId.ToString(), content);
            }
        }

        public async Task NotifyAssistantDeltaAsync(Guid conversationId, Guid personaId, string delta)
        {
            if (_connection.State != HubConnectionState.Connected)
            {
                await _connection.StartAsync();
            }
            await _connection.InvokeAsync("SendAssistantDelta", conversationId.ToString(), personaId.ToString(), delta);
        }
    }
}
