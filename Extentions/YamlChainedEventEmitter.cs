using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
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

            base.Emit(eventInfo, emitter);
        }
    }
}
