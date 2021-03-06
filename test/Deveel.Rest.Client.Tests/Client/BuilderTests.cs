﻿using System;
using System.Net.Http;

using NUnit.Framework;

namespace Deveel.Web.Client {
	[TestFixture]
	public class BuilderTests {
		[Test]
		public void BuildSettings() {
			var client = RestClient.Build(settings => settings
				.BaseUri("http://example.com/api")
				.UseJsonSerializer());

			Assert.IsNotNull(client);
			Assert.IsNotNull(client.Settings);
			Assert.IsNotNull(client.Settings.BaseUri);
			Assert.IsNotNull(client.Settings.Serializers);
			Assert.IsNotEmpty(client.Settings.Serializers);
		}

		[Test]
		public void BuildSettingsWithTypeRef() {
			var client = RestClient.Build(settings => settings
				.BaseUri("http://example.com/api")
				.UseSerializer<JsonContentSerializer>());

			Assert.IsNotNull(client);
			Assert.IsNotNull(client.Settings);
			Assert.IsNotNull(client.Settings.BaseUri);
			Assert.IsNotNull(client.Settings.Serializers);
			Assert.IsNotEmpty(client.Settings.Serializers);
		}

		[Test]
		public void BuildGetRequest() {
			var request = RestRequest.Build(builder => builder.Get().To("foo").WithQueryString("aa", 34));

			Assert.IsNotNull(request);
			Assert.IsNotNull(request.Method);
			Assert.AreEqual(HttpMethod.Get, request.Method);
			Assert.AreEqual("foo", request.Resource);
			Assert.IsNotNull(request.Parameters);
			Assert.IsTrue(request.HasQueryString());
			Assert.IsNotEmpty(request.QueryStringPairs());
			Assert.IsTrue(request.QueryStringPairs().ContainsKey("aA"));
		}

		[Test]
		public void BuildPostWithSimpleBody() {
			var request = RestRequest.Build(builder => builder.Post().To("foo").WithBody(44));

			Assert.IsNotNull(request);
			Assert.IsNotNull(request.Method);
			Assert.AreEqual(HttpMethod.Post, request.Method);
			Assert.AreEqual("foo", request.Resource);
			Assert.IsNotNull(request.Parameters);
			Assert.IsNotEmpty(request.Parameters);
			Assert.IsTrue(request.HasBody());
			Assert.IsTrue(request.Body().IsSimpleValue());
			Assert.IsInstanceOf<RequestBody>(request.Body());
		}

		[Test]
		public void BuildWithJsonBody() {
			var request = RestRequest.Build(builder => builder
				.Post()
				.To("foo")
				.WithBody(body => body
					.WithJsonContent(new {a = 22, b = "hey"})));

			Assert.IsNotNull(request);
			Assert.IsNotNull(request.Method);
			Assert.AreEqual(HttpMethod.Post, request.Method);
			Assert.AreEqual("foo", request.Resource);
			Assert.IsNotNull(request.Parameters);
			Assert.IsTrue(request.HasBody());
			Assert.IsFalse(request.Body().IsSimpleValue());
			Assert.IsInstanceOf<RequestBody>(request.Body());
			Assert.AreEqual(ContentFormat.Json, ((RequestBody)request.Body()).Format);
		}

		[Test]
		public void BuildWithJsonBodyAndReturnType() {
			var request = RestRequest.Build(builder => builder
				.Post()
				.To("foo")
				.WithJsonBody(new {a = 22, b = "hey"})
				.Returns<dynamic>());

			Assert.IsNotNull(request);
			Assert.IsNotNull(request.Method);
			Assert.AreEqual(HttpMethod.Post, request.Method);
			Assert.AreEqual("foo", request.Resource);
			Assert.IsNotNull(request.Parameters);
			Assert.IsTrue(request.HasBody());
			Assert.IsFalse(request.Body().IsSimpleValue());
			Assert.IsInstanceOf<RequestBody>(request.Body());
			Assert.AreEqual(ContentFormat.Json, ((RequestBody)request.Body()).Format);
			Assert.IsInstanceOf<dynamic>(request.Body().Value);
		}

		[Test]
		public void DeleteWithQuery() {
			var request = RestRequest.Build(builder => builder.Delete().To("user").WithQueryStrings(new {id = 22}));

			Assert.IsNotNull(request);
			Assert.AreEqual(HttpMethod.Delete, request.Method);
			Assert.IsNotNull(request.Parameters);
			Assert.IsTrue(request.HasQueryString());
			Assert.IsTrue(request.HasQueryStringPair("id"));
		}
	}
}