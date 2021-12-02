using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MtK.MongoDbDateOnlyTimeOnly
{
    public class TimeOnlySerializer : StructSerializerBase<TimeOnly>, IRepresentationConfigurable<TimeOnlySerializer>
    {
        private static TimeOnlySerializer _instance = new TimeOnlySerializer();

        public static TimeOnlySerializer Instance
        {
            get { return _instance; }
        }

        private readonly BsonType _representation;
        private readonly TimeSpanUnits _units;

        public TimeOnlySerializer(BsonType representation, TimeSpanUnits units)
        {
            switch (representation)
            {
                case BsonType.Double:
                case BsonType.Int32:
                case BsonType.Int64:
                case BsonType.String:
                    break;
                default:
                    throw new ArgumentException($"The '{representation}' representation is not a valid representation for a TimeOnlySerializer.", nameof(representation));
            }

            _representation = representation;
            _units = units;
        }

        public TimeOnlySerializer(BsonType representation) : this(representation, TimeSpanUnits.Ticks)
        {
            // left blank
        }

        public TimeOnlySerializer() : this(BsonType.String, TimeSpanUnits.Ticks)
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

        public TimeSpanUnits Units
        {
            get
            {
                return _units;
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeOnly value)
        {
            var bsonWriter = context.Writer;

            var valueAsTimeSpan = value.ToTimeSpan();

            switch (_representation)
            {
                case BsonType.Double:
                    bsonWriter.WriteDouble(ToDouble(valueAsTimeSpan, _units));
                    break;
                case BsonType.Int32:
                    bsonWriter.WriteInt32(ToInt32(valueAsTimeSpan, _units));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(ToInt64(valueAsTimeSpan, _units));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(valueAsTimeSpan.ToString());
                    break;
                default:
                    throw new BsonSerializationException($"'{_representation}' is not a valid TimeOnly representation.");
            }
        }

        public override TimeOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonReader = context.Reader;
            var bsonType = bsonReader.GetCurrentBsonType();

            TimeSpan valueAsTimeSpan;

            switch (bsonType)
            {
                case BsonType.Double:
                    valueAsTimeSpan = FromDouble(bsonReader.ReadDouble(), _units);
                    break;
                case BsonType.Int32:
                    valueAsTimeSpan = FromInt32(bsonReader.ReadInt32(), _units);
                    break;
                case BsonType.Int64:
                    valueAsTimeSpan = FromInt64(bsonReader.ReadInt64(), _units);
                    break;
                case BsonType.String:
                    valueAsTimeSpan = TimeSpan.Parse(bsonReader.ReadString());
                    break;
                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }

            return TimeOnly.FromTimeSpan(valueAsTimeSpan);
        }

        public TimeOnlySerializer WithRepresentation(BsonType representation)
        {
            if (representation == _representation)
            {
                return this;
            }
            else
            {
                return new TimeOnlySerializer(representation);
            }
        }

        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }

        // these are all taken from the TimeSpanSerializer in the official driver
        private TimeSpan FromDouble(double value, TimeSpanUnits units)
        {
            if (units == TimeSpanUnits.Nanoseconds)
            {
                return TimeSpan.FromTicks((long)(value / 100.0)); // divide first then cast to reduce chance of overflow
            }
            else
            {
                return TimeSpan.FromTicks((long)(value * TicksPerUnit(units))); // multiply first then cast to preserve fractional part of value
            }
        }

        private TimeSpan FromInt32(int value, TimeSpanUnits units)
        {
            if (units == TimeSpanUnits.Nanoseconds)
            {
                return TimeSpan.FromTicks(value / 100);
            }
            else
            {
                return TimeSpan.FromTicks(value * TicksPerUnit(units));
            }
        }

        private TimeSpan FromInt64(long value, TimeSpanUnits units)
        {
            if (units == TimeSpanUnits.Nanoseconds)
            {
                return TimeSpan.FromTicks(value / 100);
            }
            else
            {
                return TimeSpan.FromTicks(value * TicksPerUnit(units));
            }
        }

        private long TicksPerUnit(TimeSpanUnits units)
        {
            switch (units)
            {
                case TimeSpanUnits.Days: return TimeSpan.TicksPerDay;
                case TimeSpanUnits.Hours: return TimeSpan.TicksPerHour;
                case TimeSpanUnits.Minutes: return TimeSpan.TicksPerMinute;
                case TimeSpanUnits.Seconds: return TimeSpan.TicksPerSecond;
                case TimeSpanUnits.Milliseconds: return TimeSpan.TicksPerMillisecond;
                case TimeSpanUnits.Microseconds: return TimeSpan.TicksPerMillisecond / 1000;
                case TimeSpanUnits.Ticks: return 1;
                default:
                    var message = string.Format("Invalid TimeSpanUnits value: {0}.", units);
                    throw new ArgumentException(message);
            }
        }

        private double ToDouble(TimeSpan timeSpan, TimeSpanUnits units)
        {
            if (units == TimeSpanUnits.Nanoseconds)
            {
                return (double)(timeSpan.Ticks) * 100.0;
            }
            else
            {
                return (double)timeSpan.Ticks / (double)TicksPerUnit(units); // cast first then divide to preserve fractional part of result
            }
        }

        private int ToInt32(TimeSpan timeSpan, TimeSpanUnits units)
        {
            if (units == TimeSpanUnits.Nanoseconds)
            {
                return (int)(timeSpan.Ticks * 100);
            }
            else
            {
                return (int)(timeSpan.Ticks / TicksPerUnit(units));
            }
        }

        private long ToInt64(TimeSpan timeSpan, TimeSpanUnits units)
        {
            if (units == TimeSpanUnits.Nanoseconds)
            {
                return timeSpan.Ticks * 100;
            }
            else
            {
                return timeSpan.Ticks / TicksPerUnit(units);
            }
        }
    }
}
