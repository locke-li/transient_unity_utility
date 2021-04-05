using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.Networking;

namespace Transient {
    public struct HttpRequest {
        public string url;
        public byte[] data;
        public HttpMethod method;
        public Action<byte[]> WhenReceived;
        public Action<string> WhenError;
    }

    public class NetworkHttp {
        public HttpClient Client { get; private set; }

        public float Timeout {
            get => Client.Timeout.Seconds;
            set => Client.Timeout = TimeSpan.FromSeconds(value);
        }
        public string Address {
            get => Client.BaseAddress.AbsoluteUri;
            set => Client.BaseAddress = new Uri(value);
        }
        public HttpRequestHeaders DefaultHeader => Client.DefaultRequestHeaders;
        public Action<byte[]> WhenReceived { get; set; }
        public Action<string> WhenError { get; set; }
        public Action<HttpContent> WhenCompositeContent { get; set; }
        public Action WhenRequestBegin { get; set; }
        public Action WhenRequestEnd { get; set; }

        public int RetryCount { get; set; } = 3;

        private ConcurrentQueue<HttpRequest> queue;
        private Task sendTask;
        private CancellationTokenSource sendToken;

        public void Init() {
            Client = new HttpClient();
            queue = new ConcurrentQueue<HttpRequest>();
            Timeout = 5;
        }

        public void Destroy() {
            lock(queue) {
                sendToken?.Cancel();
            }
            queue = null;
            Client.Dispose();
            WhenReceived = null;
            WhenError = null;
        }

        public void Send(string url_, byte[] data_,
            Action<byte[]> WhenReceived_ = null, Action<string> WhenError_ = null,
            HttpMethod method_ = null) {
            queue.Enqueue(new HttpRequest() {
                url = url_, data = data_,
                method = method_ ?? HttpMethod.Post,
                WhenReceived = WhenReceived_ ?? WhenReceived,
                WhenError = WhenError_ ?? WhenError,
            });
            CheckRequestQueue();
        }

        public void CheckRequestQueue() {
            lock (queue) {
                if (sendTask != null) return;
                sendToken = new CancellationTokenSource();
                sendTask = SendQueued(sendToken.Token);
            }
        }

        private HttpContent CompositeContent(HttpRequest info_) {
            var content = new ByteArrayContent(info_.data);
            content.Headers.TryAddWithoutValidation("Content-Length", info_.data.Length.ToString());
            WhenCompositeContent?.Invoke(content);
            return content;
        }

        public async Task SendQueued(CancellationToken token_) {
            while(queue.TryDequeue(out var info)) {
                var request = new HttpRequestMessage(info.method, info.url) {
                    Content = CompositeContent(info)
                };
                WhenRequestBegin?.Invoke();
                var (response, error) = await SendAsync(request, token_);
                WhenRequestEnd?.Invoke();
                if (token_.IsCancellationRequested) {
                    break;
                }
                if (error != null) {
                    info.WhenError?.Invoke(error);
                    return;
                }
                var data = await response.Content.ReadAsByteArrayAsync();
                info.WhenReceived(data);
            }
            lock(queue) {
                sendTask = null;
                sendToken = null;
            }
        }

        public async Task<(HttpResponseMessage, string)> SendAsync(HttpRequestMessage request_, CancellationToken token_) {
            var retry = 0;
            HttpResponseMessage response = null;
            string error = null;
        send:
            try {
                response = await Client.SendAsync(request_, HttpCompletionOption.ResponseHeadersRead, token_);
            }
            catch(TaskCanceledException) {
                error = "timeout";
            }
            catch(HttpRequestException e) {
                error = e.Message;
            }
            catch(Exception e) {
                error = e.Message;
            }
            if (error != null && ++retry <= RetryCount && !token_.IsCancellationRequested) goto send;
            return (response, error);
        }
    }
}