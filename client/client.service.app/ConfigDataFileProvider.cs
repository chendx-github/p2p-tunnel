﻿using common.libs.database;
using common.libs.extends;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace client.service.app
{
    public class ConfigDataFileProvider<T> : IConfigDataProvider<T> where T : class
    {
        public async Task<T> Load()
        {
            string fileName = GetTableName(typeof(T));
            await Move(fileName);

            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            string content = File.ReadAllText(targetFile);
            return content.DeJson<T>();
        }
        private async Task Move(string fileName)
        {
            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            if (!File.Exists(targetFile))
            {
                string content = await ReadPackage(fileName);
                using FileStream outputStream = File.OpenWrite(targetFile);
                using StreamWriter streamWriter = new StreamWriter(outputStream, Encoding.UTF8);
                await streamWriter.WriteAsync(content);
            }
        }
        private async Task<string> ReadPackage(string fileName)
        {
            using Stream fileStream = await FileSystem.Current.OpenAppPackageFileAsync($"public/{fileName}");
            using StreamReader reader = new StreamReader(fileStream,Encoding.UTF8);
            return reader.ReadToEnd();
        }


        public async Task Save(T model)
        {
            string fileName = GetTableName(typeof(T));
            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);

            using FileStream outputStream = File.OpenWrite(targetFile);
            using StreamWriter streamWriter = new StreamWriter(outputStream, Encoding.UTF8);
            string json = model.ToJson();
            streamWriter.Write(json);
        }

        private string GetTableName(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(TableAttribute), false);
            if (attrs.Length > 0)
            {
                return $"{(attrs[0] as TableAttribute).Name}.json";
            }
            return $"{type.Name}.json";
        }
    }
}
