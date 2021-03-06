﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Deveel.Web.Client {
	public sealed class RequestBody : IMultipartBody, IBodyPart, IDisposable {
		private IDictionary<string, IBodyPart> parts;

		public RequestBody(string name, object value) 
			: this(name, value, ContentFormat.Default) {
		}

		public RequestBody() 
			: this(null) {
		}

		public RequestBody(object value) 
			: this(value, ContentFormat.Default) {
		}

		public RequestBody(object value, ContentFormat format) 
			: this(null, value, format) {
		}

		public RequestBody(string name, object value, ContentFormat format) {
			Name = name;
			Value = value;
			Format = format;
		}

		~RequestBody() {
			Dispose(false);
		}

		public string Name { get; }

		public IEnumerable<KeyValuePair<string, IBodyPart>> Parts =>
			parts == null
				? new KeyValuePair<string, IBodyPart>[0]
				: parts.ToDictionary(x => x.Key, y => y.Value).AsEnumerable();

		public ContentFormat Format { get; set; }

		RequestParameterType IRequestParameter.Type => RequestParameterType.Body;

		public object Value { get; }

		private void Dispose(bool disposing) {
			if (disposing) {
				if (parts != null) {
					var disposables = parts.OfType<IDisposable>();
					foreach (var disposable in disposables) {
						disposable.Dispose();
					}
				}
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void AddPart(IBodyPart part) {
			if (part == null)
				throw new ArgumentNullException(nameof(part));

			if (String.IsNullOrEmpty(part.Name))
				throw new ArgumentException("A part of a multi-parted body must be named");

			if (String.IsNullOrEmpty(part.Name))
				throw new ArgumentException("A part must be named");
			if (part is IRequestBody) {
				var bodyPart = (IRequestBody) part;
				if (bodyPart.IsMultiparted())
					throw new ArgumentException("Cannot add a multi-parted body to a multi-part");
				if (bodyPart.Format != ContentFormat.Default &&
					bodyPart.Format != ContentFormat.KeyValue)
					throw new ArgumentException("A body part can only contain ");
			}

			if (parts == null)
				parts = new Dictionary<string, IBodyPart>();

			if (parts.ContainsKey(part.Name))
				throw new ArgumentException($"A part named {part.Name} is already in the body");

			parts.Add(part.Name, part);
		}

		internal static HttpContent CreateContent(IRequestParameter parameter, IRestClient client) {
			if (parameter is IRequestBody &&
			    ((IRequestBody) parameter).IsMultiparted())
				return CreateMultipartContent((IMultipartBody) parameter, client);

			HttpContent content;

			if (parameter.IsBody()) {
				if (parameter.IsSimpleValue()) {
					content = parameter.ValueAsString(client);
				} else {
					var format = parameter is IRequestBody ? ((IRequestBody) parameter).Format : ContentFormat.Default;

					if (format == ContentFormat.Default)
						format = client.Settings.DefaultFormat;

					if (format == ContentFormat.Default)
						throw new NotSupportedException("Invalid content format setup: the body fallbacks to default that was not set");

					var serializer = client.Settings.Serializers.FirstOrDefault(x => x.SupportedFormat == format);
					if (serializer == null)
						throw new NotSupportedException(
							$"Unable to define a serializer for content of format {format.ToString().ToUpperInvariant()}");

					content = SerializeValue(client, parameter, serializer);
				}
			} else if (parameter.IsFile()) {
				if (!(parameter is RequestFile))
					throw new NotSupportedException();

				content = ((RequestFile) parameter).CreateFileContent(client);
			} else {
				throw new NotSupportedException();
			}

			return content;
		}

		private static HttpContent SerializeValue(IRestClient client, IRequestParameter body, IContentSerializer serializer) {
			var s = serializer.Serialize(client, body.Value);
			var contentTypes = serializer.ContentTypes;
			return new StringContent(s, Encoding.UTF8, contentTypes[0]);
		}

		private static MultipartContent CreateMultipartContent(IMultipartBody body, IRestClient client) {
			var multipart = new MultipartFormDataContent();

			foreach (var bodyPart in body.Parts) {
				var parameter = bodyPart.Value;

				HttpContent content;

				string fileName = null;
				if (parameter.IsFile()) {
					fileName = parameter.FileName();
					content = parameter.GetFileContent(client, true);
				} else {
					content = parameter.GetHttpContent(client);
				}

				if (!String.IsNullOrEmpty(fileName)) {
					multipart.Add(content, parameter.Name, fileName);
				} else {
					multipart.Add(content, parameter.Name);
				}
			}

			return multipart;
		}
	}
}