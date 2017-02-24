﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Deveel.Web.Client {
	public class RestClient : IRestClient {
		public RestClient(IRestClientSettings settings)
			: this(CreateClient(settings), settings) {
		}

		public RestClient(HttpClient client, IRestClientSettings settings) {
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			if (settings.BaseUri != null)
				client.BaseAddress = settings.BaseUri;

			if (client.BaseAddress == null)
				throw new ArgumentException("No base URI was set in the client or the settings");

			if (settings.DefaultHeaders != null) {
				foreach (var header in settings.DefaultHeaders) {
					client.DefaultRequestHeaders.Add(header.Key, (header.Value == null ? "" : header.Value.ToString()));
				}
			}

			Settings = settings;
			HttpClient = client;
		}

		public IRestClientSettings Settings { get; }

		protected HttpClient HttpClient { get; }

		private static HttpClient CreateClient(IRestClientSettings settings) {
			return settings.MessageHandler != null ? new HttpClient(settings.MessageHandler, false) : new HttpClient(); 
		}

		async Task<IRestResponse> IRestClient.RequestAsync(IRestRequest request, CancellationToken cancellationToken) {
			return await RequestAsync((RestRequest) request, cancellationToken);
		}

		public async Task<RestResponse> RequestAsync(RestRequest request, CancellationToken cancellationToken = default(CancellationToken)) {
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			if (request.Authenticate) {
				if (request.Authenticator != null) {
					request.Authenticator.AuthenticateRequest(this, request);
				} else if (Settings.Authenticator != null) {
					Settings.Authenticator.AuthenticateRequest(this, request);
				} else {
					throw new AuthenticateException($"The request {request.Method} {request.Resource} requires authentication but no authenticator was set");
				}
			}

			var httpRequest = request.AsHttpRequestMessage(this);

			if (Settings.RequestHandlers != null) {
				foreach (var handler in Settings.RequestHandlers) {
					await handler.HandleRequestAsync(this, request);
				}
			}

			var httpResponse = await HttpClient.SendAsync(httpRequest, cancellationToken);
			var response = new RestResponse(this, request, httpResponse);

			if (Settings.ResponseHandlers != null) {
				foreach (var handler in Settings.ResponseHandlers) {
					 await handler.HandleResponseAsync(this, response);
				}
			}

			return response;
		}


		protected internal virtual void OnFailResponse(HttpStatusCode statusCode, string reasonPhrase) {
			switch (statusCode) {
				case HttpStatusCode.BadRequest:
					throw new BadRequestException(reasonPhrase);
				case HttpStatusCode.Conflict:
					throw new ConflictException(reasonPhrase);
				case HttpStatusCode.Forbidden:
					throw new ForbiddenException(reasonPhrase);
				case HttpStatusCode.NotFound:
					throw new NotFoundException(reasonPhrase);
				case HttpStatusCode.Unauthorized:
					throw new UnauthorizedException(reasonPhrase);
				default:
					throw new RestResponseException(statusCode, reasonPhrase);
			}
		}

		public static RestClient Build(Action<IClientSettingsBuilder> builder) {
			var settings = new ClientSettingsBuilder();
			builder(settings);

			return new RestClient(settings.Build());
		}
	}
}