using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Text;

namespace MtK.MongoDbDateOnlyTimeOnly
{
    public class DateOnlySerializer : StructSerializerBase<DateOnly>, IRepresentationConfigurable<DateOnlySerializer>
    {
        private static DateOnlySerializer _instance = new DateOnlySerializer();

        public static DateOnlySerializer Instance
        {
            get { return _instance; }
        }

        private readonly BsonType _representation;

        public DateOnlySerializer(BsonType representation)
        {
            switch (representation)
            {
                case BsonType.Int32:
                case BsonType.Document:
                case BsonType.String:
                case BsonType.DateTime:
                    break;
                default:
                    throw new ArgumentException($"The {representation} representation is not a valid representation for a DateOnlySerializer.", nameof(representation));
            }

            _representation = representation;
        }

        public DateOnlySerializer() : this(BsonType.String)
        {
            // left blank
        }

        public BsonType Representation
        {
            get
            {
                return _representation;
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
        {
            var bsonWriter = context.Writer;

            switch (_representation)
            {
                case BsonType.Document:
                    bsonWriter.WriteStartDocument();

                    bsonWriter.WriteInt32("Year", value.Year);
                    bsonWriter.WriteInt32("Month", value.Month);
                    bsonWriter.WriteInt32("Day", value.Day);

                    bsonWriter.WriteEndDocument();

                    break;
                case BsonType.String:
                    bsonWriter.WriteString(value.ToString("yyyy-MM-dd"));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(value.DayNumber);
                    break;
                case BsonType.DateTime:
                    var valueAsUtcDateTime = value.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero), DateTimeKind.Utc);
                    var millisecondsSinceEpoch = BsonUtils.ToMillisecondsSinceEpoch(valueAsUtcDateTime);

                    bsonWriter.WriteDateTime(millisecondsSinceEpoch);
                    
                    break;
                default:
                    throw new BsonSerializationException($"'{_representation}' is not a valid DateOnly representation.");
            }
        }

        public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;
            var bsonType = bsonReader.GetCurrentBsonType();

            DateOnly value;

            switch (bsonType)
            {
                case BsonType.Document:
                    bsonReader.ReadStartDocument();

                    Dictionary<string, int> values = new Dictionary<string, int>();

                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = bsonReader.ReadName();

                        values.Add(name, BsonSerializer.Deserialize<int>(bsonReader));
                    }

                    bsonReader.ReadEndDocument();

                    if(values.ContainsKey("Year") && values.ContainsKey("Month") && values.ContainsKey("Day"))
                    {
                        value = new DateOnly(values["Year"], values["Month"], values["Day"]);
                    }
                    else
                    {
                        throw CreateCannotBeDeserializedException();
                    }

                    break;
                case BsonType.String:
                    value = DateOnly.ParseExact(bsonReader.ReadString(), "yyyy-MM-dd");

                    break;
                case BsonType.Int32:
                    value = DateOnly.FromDayNumber(bsonReader.ReadInt32());
                    break;
                case BsonType.DateTime:
                    var fullValue = new BsonDateTime(bsonReader.ReadDateTime()).ToUniversalTime(); // ensures proper handling of MinValue and MaxValue
                    value = DateOnly.FromDateTime(fullValue); // TODO: we may want to enforce a zero time portion, perhaps as an optional configuration on the serializer for flexibility

                    break;
                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }

            return value;
        }

        public DateOnlySerializer WithRepresentation(BsonType representation)
        {
            if (representation == _representation)
            {
                return this;
            }
            else
            {
                return new DateOnlySerializer(representation);
            }
        }

        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
}
