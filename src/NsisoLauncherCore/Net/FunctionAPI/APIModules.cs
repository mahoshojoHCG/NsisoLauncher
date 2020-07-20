﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NsisoLauncherCore.Net.FunctionAPI
{
    public class APIModules
    {
        public class JWForge
        {
            /// <summary>
            ///     Build
            /// </summary>
            [JsonProperty("build")]
            public int Build { get; set; }

            /// <summary>
            ///     版本号
            /// </summary>
            [JsonProperty("version")]
            public string Version { get; set; }
        }

        public class JWJava
        {
            /// <summary>
            ///     JAVA标题
            /// </summary>
            [JsonProperty("title")]
            public string Title { get; set; }

            /// <summary>
            ///     JAVA文件
            /// </summary>
            [JsonProperty("file")]
            public string File { get; set; }
        }

        public class JWNews
        {
            /// <summary>
            ///     新闻标题
            /// </summary>
            [JsonProperty("title")]
            public string Title { get; set; }

            /// <summary>
            ///     新闻类型
            /// </summary>
            [JsonProperty("classify")]
            public string Classify { get; set; }

            /// <summary>
            ///     新闻时间
            /// </summary>
            [JsonProperty("time")]
            public string Time { get; set; }

            /// <summary>
            ///     新闻链接
            /// </summary>
            [JsonProperty("link")]
            public string Link { get; set; }
        }

        public class JWVersions
        {
            /// <summary>
            ///     版本集合
            /// </summary>
            [JsonProperty("versions")]
            public List<JWVersion> Versions { get; set; }
        }

        public class JWVersion
        {
            /// <summary>
            ///     版本ID
            /// </summary>
            [JsonProperty("id")]
            public string Id { get; set; }

            /// <summary>
            ///     版本类型
            /// </summary>
            [JsonProperty("type")]
            public string Type { get; set; }

            /// <summary>
            ///     版本修改时间
            /// </summary>
            [JsonProperty("time")]
            public string Time { get; set; }

            /// <summary>
            ///     版本发布时间
            /// </summary>
            [JsonProperty("releaseTime")]
            public string ReleaseTime { get; set; }

            /// <summary>
            ///     版本下载URL
            /// </summary>
            [JsonProperty("url")]
            public string Url { get; set; }
        }

        public class JLauncherVersion
        {
            [JsonProperty("id")] public string ID { get; set; }

            [JsonProperty("version")] public string Version { get; set; }

            [JsonProperty("type")] public string Type { get; set; }

            [JsonProperty("url_main")] public string UrlMain { get; set; }

            [JsonProperty("url_backup")] public string UrlBackup { get; set; }

            [JsonProperty("description")] public string Description { get; set; }

            [JsonProperty("interpret")] public string Interpret { get; set; }

            [JsonProperty("release_time")] public DateTime ReleaseTime { get; set; }
        }
    }
}