using Newtonsoft.Json;
using System;
using System.Text;

namespace WiiUDatabaseUpdater.Eshop
{
    class StringEscapeJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JsonTextWriter textWriter = (JsonTextWriter)writer;
            string quote = textWriter.QuoteChar.ToString();
            string str = (string)value;
            str = quote + EscapeString(str) + quote;
            textWriter.WriteRawValue(str);
        }

        private string EscapeString(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                switch (c)
                {
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '/':
                        sb.Append("\\/");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    default:
                        if (c < 0x20 || c > 0x7e)
                            sb.Append(String.Format("\\u{0:x4}", (int)c));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
