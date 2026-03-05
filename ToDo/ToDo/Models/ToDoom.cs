using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDo.Models
{
    class ToDoom: INotifyPropertyChanged // привязка данных
    {
        private bool _isDone;
        private string _Text;

        public DateTime CreatData { get; set; } = DateTime.Now;  // создание даты 
        public bool IsDone 
        { 
            get { return _isDone; }
            set 
            {
                if (_isDone == value) // проверяем на совпадение 
                    return;
                _isDone = value;
                OnPropertyChanged("IsDone"); // уведомление об изменении
            }
        }

        public string Text
        {
            get { return _Text; }
            set 
            {
                if (_Text == value)
                    return;
                _Text = value;
                OnPropertyChanged("Text");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = "") // изменяем событие 
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }   
    
}
