using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDo.Models;

namespace ToDo.Services
{
    class FileService // загрузки и сохр данных в JSON
    {
        private readonly string PATH;

        public FileService(string path)
        {
            PATH = path;
        }
        public BindingList<ToDoom> LoadData()
        {
            var fileExists = File.Exists(PATH);
            if (!fileExists)
            {
                File.CreateText(PATH).Dispose(); // создаём пустой файл если не существует
                return new BindingList<ToDoom>(); // возвр пустой список
            }
            using (var reader = File.OpenText(PATH))
            {
                var fileText = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<BindingList<ToDoom>>(fileText); // десериализуем JSON в Bndlist
            }
            return null;
        }

        public void SaveData(object data) //сохр
        {
            using (StreamWriter writer = File.CreateText(PATH))
            {
                string output = JsonConvert.SerializeObject(data);
                writer.WriteLine(output);
            }
        }
    }
}
