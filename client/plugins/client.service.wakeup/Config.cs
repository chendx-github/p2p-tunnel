﻿using common.libs.database;
using common.libs.extends;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.wakeup
{
    /// <summary>
    /// 
    /// </summary>
    [Table("wakeup-appsettings")]
    public sealed class Config
    {
        /// <summary>
        /// 
        /// </summary>
        public Config() { }
        private readonly IConfigDataProvider<Config> configDataProvider;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configDataProvider"></param>
        public Config(IConfigDataProvider<Config> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            Config config = ReadConfig().Result;
            Items = config.Items;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<ConfigItem> Items { get; set; } = new List<ConfigItem>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<bool> Add(int index, ConfigItem item)
        {
            try
            {
                if (index >= 0)
                {
                    ConfigItem old = Items.ElementAt(index);
                    old.Mac = item.Mac;
                    old.Name = item.Name;
                }
                else
                {
                    Items.Add(item);
                }

                await SaveConfig(this.ToJson());

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public async Task<bool> Remove(int index)
        {
            try
            {
                Items.RemoveAt(index);

                await SaveConfig(this.ToJson());

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<Config> ReadConfig()
        {
            return await configDataProvider.Load();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public async Task SaveConfig(string jsonStr)
        {
            var _config = jsonStr.DeJson<Config>();
            Items = _config.Items;

            await configDataProvider.Save(jsonStr).ConfigureAwait(false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>(64);
            foreach (var item in Items)
            {
                var macBytes = item.Mac.GetUTF16Bytes();
                var nameBytes = item.Name.GetUTF16Bytes();

                bytes.Add((byte)macBytes.Length);
                bytes.Add((byte)item.Mac.Length);
                bytes.AddRange(macBytes.ToArray());
                bytes.Add((byte)nameBytes.Length);
                bytes.Add((byte)item.Name.Length);
                bytes.AddRange(nameBytes.ToArray());
            }
            return bytes.ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memory"></param>
        /// <returns></returns>
        public static List<ConfigItem> DeBytes(ReadOnlyMemory<byte> memory)
        {
            var span = memory.Span;
            List<ConfigItem> res = new List<ConfigItem>();

            int index = 0;
            while (index < span.Length - 1)
            {
                string mac = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
                index += 1 + 1 + span[index];
                string name = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
                index += 1 + 1 + span[index];

                res.Add(new ConfigItem
                {
                    Mac = mac,
                    Name = name,
                });
            }
            return res;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ConfigItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Mac { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
