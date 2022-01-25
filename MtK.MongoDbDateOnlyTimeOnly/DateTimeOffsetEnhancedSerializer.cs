using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MtK.MongoDbHelpfulSerializers
{
    public class DateTimeOffsetEnhancedSerializer : DateTimeOffsetSerializer, IRepresentationConfigurable<DateTimeOffsetEnhancedSerializer>
    {
        private static class Flags
        {
            public const long DateTime = 1;
            public const long Ticks = 2;
            public const long Offset = 4;
        }

        private readonly SerializerHelper _helper;

        protected readonly BsonType _representation;
        
        public DateTimeOffsetEnhancedSerializer() : this(BsonType.DateTime)
        {
            
        }

        public DateTimeOffsetEnhancedSerializer(BsonType representation)
        {
            switch (representation)
            {

                case BsonType.DateTime:
                case BsonType.Array:
                case BsonType.Document:
                case BsonType.String:
                    break;
                default:
                    var message = string.Format("{0} is not a valid representation for a DateTimeOffsetSerializer.", representation);
                    throw new ArgumentException(message);
            }

            _representation = representation;
            _helper = new SerializerHelper
                (
                    new SerializerHelper.Member("DateTime", Flags.DateTime),
                    new SerializerHelper.Member("Ticks", Flags.Ticks),
                    new SerializerHelper.Member("Offset", Flags.Offset)
                );
        }

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override DateTimeOffset Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;
            
            BsonType bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.DateTime:
                    var ticks = bsonReader.ReadDateTime();

                    //Debug.WriteLine($"before ticks: {ticks}");
                    
                    if(ticks < DateTimeOffset.MinValue.Ticks)
                    {
                        ticks = DateTimeOffset.MinValue.Ticks;
                    }

                    //Debug.WriteLine($"after ticks: {ticks}");
                    return DateTimeOffset.FromUnixTimeMilliseconds(ticks);
                default:
                    return base.Deserialize(context, args);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTimeOffset value)
        {
            var bsonWriter = context.Writer;

            // note: the DateTime portion cannot be serialized as a BsonType.DateTime because it is NOT in UTC

            switch (_representation)
            {
                case BsonType.DateTime:
                    bsonWriter.WriteDateTime(BsonUtils.ToMillisecondsSinceEpoch(value.UtcDateTime));
                    break;
                default:
                    base.Serialize(context, args, value);
                    break;
            }
        }

        public new BsonType Representation
        {
            get
            {
                return _representation;
            }
        }

        public new DateTimeOffsetEnhancedSerializer WithRepresentation(BsonType representation)
        {
            if(representation == _representation)
            {
                return this;
            }
            else
            {
                return new DateTimeOffsetEnhancedSerializer(representation);
            }
        }

        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
}
