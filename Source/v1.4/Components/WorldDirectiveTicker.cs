using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace MechHumanlikes
{
    public class WorldDirectiveTicker : WorldComponent
    {
        List<Directive> tickDirectives = new List<Directive>();

        List<List<Directive>> rareTickDirectives = new List<List<Directive>>();

        List<List<Directive>> longTickDirectives = new List<List<Directive>>();

        Dictionary<Directive, TickerType> directivesToRegister = new Dictionary<Directive, TickerType>();

        Dictionary<Directive, TickerType> directivesToDeregister = new Dictionary<Directive, TickerType>();

        public WorldDirectiveTicker(World world) : base(world)
        {
            for (int i = GenTicks.TickRareInterval; i > 0; i--)
            {
                rareTickDirectives.Add(new List<Directive>());
            }
            for (int i = GenTicks.TickLongInterval; i > 0; i--)
            {
                longTickDirectives.Add(new List<Directive>());
            }
        }

        public override void WorldComponentTick()
        {
            foreach (Directive directive in directivesToRegister.Keys)
            {
                BucketOf(directivesToRegister[directive], directive).Add(directive);
            }
            directivesToRegister.Clear();

            foreach (Directive directive in directivesToDeregister.Keys)
            {
                BucketOf(directivesToDeregister[directive], directive).Remove(directive);
            }
            directivesToDeregister.Clear();

            List<Directive> directives = tickDirectives;
            for (int i = directives.Count - 1; i >= 0; i--)
            {
                directives[i].Tick();
            }

            directives = rareTickDirectives[Find.TickManager.TicksGame % GenTicks.TickRareInterval];
            for (int i = directives.Count - 1; i >= 0; i--)
            {
                directives[i].TickRare();
            }

            directives = longTickDirectives[Find.TickManager.TicksGame % GenTicks.TickLongInterval];
            for (int i = directives.Count - 1; i >= 0; i--)
            {
                directives[i].TickLong();
            }

            if (GenTicks.TicksGame % GenTicks.TickLongInterval == 0)
            {
                foreach (Map map in Find.Maps)
                {
                    MDR_Utils.UpdateAmicableDroneCount(map);
                }
            }
        }

        public void RegisterDirective(Directive directive, TickerType tickType)
        {
            directivesToRegister.Add(directive, tickType);
        }

        public void DeregisterDirective(Directive directive, TickerType tickType)
        {
            directivesToDeregister.Add(directive, tickType);
        }

        private List<Directive> BucketOf(TickerType type, Directive directive)
        {
            int hashCode = directive.pawn.GetHashCode();
            if (hashCode < 0)
            {
                hashCode *= -1;
            }

            switch (type)
            {
                case TickerType.Normal:
                    return tickDirectives;
                case TickerType.Rare:
                    return rareTickDirectives[hashCode % GenTicks.TickRareInterval];
                case TickerType.Long:
                    return longTickDirectives[hashCode % GenTicks.TickLongInterval];
                default:
                    return null;
            }
        }
    }
}
