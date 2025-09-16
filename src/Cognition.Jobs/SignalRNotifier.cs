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

        public async Task NotifyAssistantMessageAsync(Guid conversationId, string content)
        {
            await _connection.InvokeAsync("SendAssistantMessage", conversationId.ToString(), content);
        }
    }
}
