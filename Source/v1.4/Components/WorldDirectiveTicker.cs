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
            for (int i = 250; i > 0; i--)
            {
                rareTickDirectives.Add(new List<Directive>());
            }
            for (int i = 2000; i > 0; i--)
            {
                longTickDirectives.Add(new List<Directive>());
            }
        }

        public override void WorldComponentTick()
        {
            foreach (Directive directive in directivesToRegister.Keys)
            {
                Log.Warning("Directive " + directive.def.defName + " was registered and is being given a bucket now.");
                List<Directive> bucket = BucketOf(directivesToRegister[directive], directive);
                bucket.Add(directive);
                Log.Warning("Directive " + directive.def.defName + " was given a bucket whose count is now " + bucket.Count);
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
                if (Find.TickManager.TicksGame % 100 == 0)
                {
                    Log.Warning("Normal ticker for " + directives[i].def.defName + " is working.");
                }
                directives[i].Tick();
            }

            directives = rareTickDirectives[Find.TickManager.TicksGame % 250];
            for (int i = directives.Count - 1; i >= 0; i--)
            {
                Log.Warning("Normal ticker for " + directives[i].def.defName + " is working.");
                directives[i].TickRare();
            }

            directives = longTickDirectives[Find.TickManager.TicksGame % 2000];
            for (int i = directives.Count - 1; i >= 0; i--)
            {
                Log.Warning("Normal ticker for " + directives[i].def.defName + " is working.");
                directives[i].TickLong();
            }
        }

        public void RegisterDirective(Directive directive, TickerType tickType)
        {
            directivesToRegister.Add(directive, tickType);
            Log.Warning("Directive " + directive.def.defName + " has requested registration for " + tickType);
        }

        public void DeregisterDirective(Directive directive, TickerType tickType)
        {
            directivesToDeregister.Add(directive, tickType);
            Log.Warning("Directive " + directive.def.defName + " has requested deregistration for " + tickType);
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
                    Log.Warning("Directive " + directive.def.defName + " has matched normal ticking.");
                    return tickDirectives;
                case TickerType.Rare:
                    Log.Warning("Directive " + directive.def.defName + " has matched rare ticking at the " + hashCode % 250 + " interval mark.");
                    return rareTickDirectives[hashCode % 250];
                case TickerType.Long:
                    Log.Warning("Directive " + directive.def.defName + " has matched long ticking at the " + hashCode % 2000 + " interval mark.");
                    return longTickDirectives[hashCode % 2000];
                default:
                    return null;
            }
        }

        private int TickInterval(TickerType type)
        {
            switch (type)
            {
                case TickerType.Normal:
                    return 1;
                case TickerType.Rare:
                    return 250;
                case TickerType.Long:
                    return 2000;
                default:
                    return -1;
            }
        }
    }
}
