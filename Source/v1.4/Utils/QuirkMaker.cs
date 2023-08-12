

using System.Security.Cryptography;
using System;
using Verse;

namespace MechHumanlikes
{
    public static class DirectiveMaker
    {
        public static Directive MakeDirective(DirectiveDef def, Pawn pawn)
        {
            Directive directive = (Directive)Activator.CreateInstance(def.directiveClass);
            directive.def = def;
            directive.pawn = pawn;
            directive.PostMake();
            return directive;
        }
    }
}
