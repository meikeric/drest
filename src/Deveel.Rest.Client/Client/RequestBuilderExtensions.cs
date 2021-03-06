﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Deveel.Web.Client {
	public static class RequestBuilderExtensions {
		public static IRequestBuilder Method(this IRequestBuilder builder, string method) {
			if (String.IsNullOrEmpty(method))
				throw new ArgumentNullException(nameof(method));

			HttpMethod httpMethod;
			switch (method.ToUpperInvariant()) {
				case "GET":
					httpMethod = HttpMethod.Get;
					break;
				case "POST":
					httpMethod = HttpMethod.Post;
					break;
				case "DELETE":
					httpMethod = HttpMethod.Delete;
					break;
				case "PUT":
					httpMethod = HttpMethod.Put;
					break;
				case "HEAD":
					httpMethod = HttpMethod.Head;
					break;
				case "OPTIONS":
					httpMethod = HttpMethod.Options;
					break;
				default:
					throw new ArgumentException($"The string {method} is not a valid HTTP method");
			}

			return builder.Method(httpMethod);
		}

		public static IRequestBuilder Get(this IRequestBuilder builder) {
			return builder.Method(HttpMethod.Get);
		}

		public static IRequestBuilder Post(this IRequestBuilder builder) {
			return builder.Method(HttpMethod.Post);
		}

		public static IRequestBuilder Put(this IRequestBuilder builder) {
			return builder.Method(HttpMethod.Put);
		}

		public static IRequestBuilder Delete(this IRequestBuilder builder) {
			return builder.Method(HttpMethod.Delete);
		}

		public static IRequestBuilder To(this IRequestBuilder builder, string format, params object[] args) {
			if (String.IsNullOrEmpty(format))
				throw new ArgumentNullException(nameof(format));
			if (args == null)
				throw new ArgumentNullException(nameof(args));

			for (int i = 0; i < args.Length; i++) {
				builder = builder.WithRoute(i.ToString(), args[i]);
			}

			return builder.To(format);
		}

		public static IRequestBuilder With(this IRequestBuilder builder, params IRequestParameter[] parameters) {
			foreach (var parameter in parameters) {
				builder = builder.With(parameter);
			}

			return builder;
		}

		public static IRequestBuilder With(this IRequestBuilder builder, RequestParameterType type, string name, object value) {
			IRequestParameter parameter;
			if (type == RequestParameterType.Body) {
				parameter = new RequestBody(name, value);
			} else if (type == RequestParameterType.File) {
				if (!(value is Stream))
					throw new ArgumentException();

				parameter = new RequestFile(name, name, (Stream)value);
			} else {
				if (String.IsNullOrEmpty(name))
					throw new ArgumentNullException(nameof(name));

				parameter = new SimpleRequestParameter(type, name, value);
			}

			return builder.With(parameter);
		}

		public static IRequestBuilder WithRoute(this IRequestBuilder builder, string name, object value) {
			return builder.With(RequestParameterType.Route, name, value);
		}

		public static IRequestBuilder WithQueryString(this IRequestBuilder builder, string name, object value) {
			return builder.With(RequestParameterType.QueryString, name, value);
		}

		public static IRequestBuilder WithHeader(this IRequestBuilder builder, string name, object value) {
			return builder.With(RequestParameterType.Header, name, value);
		}

		public static IRequestBuilder With(this IRequestBuilder builder, RequestParameterType type, IEnumerable<KeyValuePair<string, object>> values) {
			foreach (var pair in values) {
				builder = builder.With(type, pair.Key, pair.Value);
			}

			return builder;
		}

		public static IRequestBuilder WithQueryStrings(this IRequestBuilder builder, IEnumerable<KeyValuePair<string, object>> values) {
			return builder.With(RequestParameterType.QueryString, values);
		}

		public static IRequestBuilder WithRoutes(this IRequestBuilder builder, IEnumerable<KeyValuePair<string, object>> values) {
			return builder.With(RequestParameterType.Route, values);
		}

		public static IRequestBuilder WithHeaders(this IRequestBuilder builder, IEnumerable<KeyValuePair<string, object>> values) {
			return builder.With(RequestParameterType.Header, values);
		}

		public static IRequestBuilder With(this IRequestBuilder builder, RequestParameterType type, object values) {
			if (values != null)
				builder = builder.With(values.AsParameters(type).ToArray());

			return builder;
		}

		public static IRequestBuilder WithQueryStrings(this IRequestBuilder builder, object values) {
			return builder.With(RequestParameterType.QueryString, values);
		}

		public static IRequestBuilder WithHeaders(this IRequestBuilder builder, object values) {
			return builder.With(RequestParameterType.Header, values);
		}

		public static IRequestBuilder WithRoutes(this IRequestBuilder builder, object values) {
			return builder.With(RequestParameterType.Route, values);
		}

		public static IRequestBuilder WithBody(this IRequestBuilder builder, string name, object value) {
			return builder.With(RequestParameterType.Body, name, value);
		}

		public static IRequestBuilder WithBody(this IRequestBuilder builder, object value) {
			return builder.WithBody(null, value);
		}

		public static IRequestBuilder WithBody(this IRequestBuilder builder, IRequestBody body) {
			return builder.With(body);
		}

		public static IRequestBuilder WithBody(this IRequestBuilder builder, Action<IRequestBodyBuilder> body) {
			var bodyBuilder = new RequestBodyBuilder();
			body(bodyBuilder);

			return builder.WithBody(bodyBuilder.Build());
		}

		public static IRequestBuilder WithJsonBody(this IRequestBuilder builder, object value) {
			return builder.WithBody(body => body.WithJsonContent(value));
		}

		public static IRequestBuilder WithXmlBody(this IRequestBuilder builder, object value) {
			return builder.WithBody(body => body.WithXmlContent(value));
		}

		public static IRequestBuilder WithFile(this IRequestBuilder builder, IRequestFile file) {
			return builder.With(file);
		}

		public static IRequestBuilder WithFile(this IRequestBuilder builder, Action<IRequestFileBuilder> file) {
			var fileBuilder = new RequestFileBuilder();
			file(fileBuilder);

			return builder.WithFile(fileBuilder.Build());
		}

		public static IRequestBuilder Returns<T>(this IRequestBuilder builder) {
			return builder.Returns(RequestReturn.Object(typeof(T)));
		}

		public static IRequestBuilder Returns(this IRequestBuilder builder, Type returnedType) {
			return builder.Returns(RequestReturn.Object(returnedType));
		}

		public static IRequestBuilder ReturnsDefaultFormat(this IRequestBuilder builder) {
			return builder.ReturnsFormat(ContentFormat.Default);
		}

		public static IRequestBuilder ReturnsJson(this IRequestBuilder builder) {
			return builder.ReturnsFormat(ContentFormat.Json);
		}

		public static IRequestBuilder ReturnsXml(this IRequestBuilder builder) {
			return builder.ReturnsFormat(ContentFormat.Xml);
		}

		public static IRequestBuilder ReturnsJson(this IRequestBuilder builder, Type returnType) {
			return builder.Returns(returnType).ReturnsFormat(ContentFormat.Json);
		}

		public static IRequestBuilder ReturnsJson<T>(this IRequestBuilder builder) {
			return builder.ReturnsJson(typeof(T));
		}

		public static IRequestBuilder ReturnsXml(this IRequestBuilder builder, Type returnType) {
			return builder.Returns(returnType).ReturnsFormat(ContentFormat.Xml);
		}

		public static IRequestBuilder ReturnsXml<T>(this IRequestBuilder builder) {
			return builder.ReturnsXml(typeof(T));
		}

		public static IRequestBuilder ReturnsFile(this IRequestBuilder builder, string contentType) {
			return builder.Returns(RequestReturn.File(contentType));
		}

		public static IRequestBuilder ReturnsFile(this IRequestBuilder builder) {
			return builder.Returns(RequestReturn.File());
		}

		public static IRequestBuilder HasNoReturn(this IRequestBuilder builder) {
			return builder.Returns(RequestReturn.Void);
		}

		public static IRequestBuilder UseBasicAuthentication(this IRequestBuilder builder, string userName, string password) {
			return builder.UseAuthenticator(new BasicRequestAuthenticator(userName, password));
		}

		public static IRequestBuilder UseJwtAuthentication(this IRequestBuilder builder, string token) {
			return builder.UseAuthenticator(new JwtRequestAuthenticator(token));
		}

		public static IRequestBuilder Anonymous(this IRequestBuilder builder) {
			return builder.UseAuthenticator(null).Authenticate(false);
		}
	}
}