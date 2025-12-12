using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Cognition.Jobs
{
    public class SignalRNotifier : IPlanProgressNotifier
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
            await EnsureConnectedAsync();
        }

        public async Task NotifyAssistantMessageAsync(Guid conversationId, Guid personaId, string content, Guid? messageId = null)
        {
            if (!await EnsureConnectedAsync().ConfigureAwait(false)) return;

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
            if (!await EnsureConnectedAsync().ConfigureAwait(false)) return;

            await _connection.InvokeAsync("SendAssistantDelta", conversationId.ToString(), personaId.ToString(), delta);
        }

        public async Task NotifyPlanProgressAsync(Guid conversationId, object payload)
        {
            if (!await EnsureConnectedAsync().ConfigureAwait(false)) return;

            await _connection.InvokeAsync("SendPlanProgress", conversationId.ToString(), payload);
        }

        private async Task<bool> EnsureConnectedAsync()
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                return true;
            }

            try
            {
                await _connection.StartAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalRNotifier: failed to connect to {_hubUrl}: {ex.Message}");
                return false;
            }

            return _connection.State == HubConnectionState.Connected;
        }
    }
}
