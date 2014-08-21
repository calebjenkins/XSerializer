﻿using System;
using System.Xml;

namespace XSerializer
{
    internal class XmlElementSerializer<T> : IXmlSerializerInternal<T>
    {
        private readonly string _elementName;
        private readonly bool _alwaysEmitNil;
        private readonly IValueConverter _valueConverter;

        public XmlElementSerializer(IXmlSerializerOptions options)
        {
            if (!typeof(T).IsPrimitiveLike() && !typeof(T).IsNullablePrimitiveLike() && typeof(T) != typeof(Enum) && typeof(T) != typeof(Type) && typeof(T) != typeof(Uri))
            {
                throw new InvalidOperationException("Generic argument of XmlElementSerializer<T> must be an primitive, like a primitive (e.g. Guid, DateTime), a nullable of either, or one of: Enum, Type, or Uri.");
            }

            _elementName = options.RootElementName;
            _alwaysEmitNil = options.ShouldAlwaysEmitNil;

            if (typeof(T) == typeof(Enum))
            {
                _valueConverter = new EnumTypeValueConverter(options.RedactAttribute, options.ExtraTypes);
            }
            else if (typeof(T) == typeof(Type))
            {
                _valueConverter = new TypeTypeValueConverter(options.RedactAttribute);
            }
            else if (typeof(T) == typeof(Uri))
            {
                _valueConverter = new UriTypeValueConverter(options.RedactAttribute);
            }
            else
            {
                _valueConverter = SimpleTypeValueConverter.Create(typeof(T), options.RedactAttribute);
            }
        }

        public void Serialize(SerializationXmlTextWriter writer, T value, ISerializeOptions options)
        {
            SerializeObject(writer, value, options);
        }

        public void SerializeObject(SerializationXmlTextWriter writer, object value, ISerializeOptions options)
        {
            var wasEmptyWriter = writer.IsEmpty;
            writer.WriteStartDocument();

            if (value != null)
            {
                WriteElement(writer, w => w.WriteValue(_valueConverter.GetString(value, options)));
            }
            else if (_alwaysEmitNil || options.ShouldEmitNil || wasEmptyWriter)
            {
                WriteElement(writer, w => w.WriteNilAttribute());
            }
        }

        private void WriteElement(SerializationXmlTextWriter writer, Action<SerializationXmlTextWriter> writeValueAction)
        {
            writer.WriteStartElement(_elementName);
            writer.WriteDefaultDocumentNamespaces();
            writeValueAction(writer);
            writer.WriteEndElement();
        }

        public T Deserialize(XmlReader reader)
        {
            if (typeof(T) == typeof(Enum)
                || typeof(T) == typeof(Type)
                || typeof(T) == typeof(Uri))
            {
                while (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                }
            }

            if (reader.IsNil())
            {
                return default(T);
            }

            var value = reader.ReadString();
            return (T)_valueConverter.ParseString(value);
        }

        public object DeserializeObject(XmlReader reader)
        {
            return Deserialize(reader);
        }
    }
}