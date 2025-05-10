using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Quest_Data_Builder.Extentions
{
    internal class YamlChainedEventEmitter : ChainedEventEmitter
    {
        public YamlChainedEventEmitter(IEventEmitter nextEmitter) : base(nextEmitter) { }

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            if (eventInfo.Source.Type == typeof(string))
            {
                var value = eventInfo.Source.Value as string ?? string.Empty;

                eventInfo = new ScalarEventInfo(eventInfo.Source)
                {
                    Style = ScalarStyle.DoubleQuoted
                };
            }

            if (eventInfo.Source.Type == typeof(double) || eventInfo.Source.Type == typeof(float))
            {
                var number = Convert.ToDouble(eventInfo.Source.Value);
                var formatted = number % 1 == 0
                    ? number.ToString("0.0", CultureInfo.InvariantCulture)
                    : number.ToString("0.################", CultureInfo.InvariantCulture);

                var scalar = new Scalar(null, null, formatted, ScalarStyle.Any, true, false);
                emitter.Emit(scalar);
                return;
            }

            base.Emit(eventInfo, emitter);
        }
    }
}
