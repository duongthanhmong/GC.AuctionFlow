using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace GC.AuctionFlow.Tests.Unit.Runtime;

/// <summary>
/// B0-S2 R7 authority guard engine. Reads the COMPILED IL of GC.AuctionFlow.dll and follows a
/// transitive, runtime-dispatch-closed, ORDER-INDEPENDENT closure from a set of seed methods,
/// resolving every referenced type/member by STRUCTURED metadata — ownership is decided by resolved
/// namespace/type identity, never by identifier spelling and never by substring.
///
/// R7 hardening over R6:
///  - TWO-PASS metadata indexing: pass 1 collects every TypeDefinition/method/flag; pass 2 resolves
///    base classes, interfaces and MethodImpl — so relations never depend on TypeDefinition order;
///  - STRUCTURED supertypes with generic substitution: a generic base/interface's concrete type
///    arguments are substituted into inherited field/contract types, so an inherited generic carrier
///    (NeutralContext : EvidenceBase&lt;EffortResultSnapshot&gt;) is classified correctly;
///  - GENERIC FIELD DECLARING TYPE: a field access also inspects the field's declaring type spec, so
///    a static generic holder (NeutralHolder&lt;EffortResultSnapshot&gt;.Current) is caught by the
///    forbidden type argument in the declaring TypeSpecification.
/// Plus the R6 coverage (interface/virtual dispatch closure, MethodSpecification, delegate edges,
/// method contracts, carrier fixpoint, exact overload resolution). Any unresolved opcode / internal
/// call target / ambiguous overload THROWS.
/// </summary>
internal static class MetadataAuthorityGraph
{
    internal sealed record Finding(string Method, string ReferencedType, string Reason);

    internal readonly record struct Named(string Namespace, string FullName);

    internal sealed record TypeSig
    {
        public required string Kind;                 // named|wrap|primitive|genericParam|fnptr
        public string? Namespace;
        public string? FullName;
        public ImmutableArray<TypeSig> Args = ImmutableArray<TypeSig>.Empty;
        public TypeSig? Element;
        public int GenericIndex = -1;                // for genericParam
        public bool IsMethodParam;                   // genericParam: !!i (method) vs !i (type)
    }

    private static IEnumerable<Named> NamedNodes(TypeSig? t)
    {
        if (t is null) yield break;
        if (t.Kind == "named" && t.FullName != null)
            yield return new Named(t.Namespace ?? "", t.FullName);
        foreach (var a in t.Args)
            foreach (var n in NamedNodes(a)) yield return n;
        foreach (var n in NamedNodes(t.Element)) yield return n;
    }

    // named identities in a type's generic arguments / element only (NOT the root named type itself)
    private static IEnumerable<Named> ArgNodes(TypeSig t)
    {
        foreach (var a in t.Args)
            foreach (var n in NamedNodes(a)) yield return n;
        foreach (var n in NamedNodes(t.Element)) yield return n;
    }

    // substitute concrete type arguments for type generic parameters (!i); method params (!!i) untouched
    private static TypeSig? Subst(TypeSig? t, ImmutableArray<TypeSig> args)
    {
        if (t is null) return null;
        switch (t.Kind)
        {
            case "genericParam":
                return (!t.IsMethodParam && t.GenericIndex >= 0 && t.GenericIndex < args.Length) ? args[t.GenericIndex] : t;
            case "named":
                return t.Args.IsEmpty ? t : t with { Args = t.Args.Select(a => Subst(a, args)!).ToImmutableArray() };
            case "wrap":
                return t with { Element = Subst(t.Element, args) };
            default:
                return t;
        }
    }

    private sealed class SigTypeProvider : ISignatureTypeProvider<TypeSig, object?>
    {
        private readonly MetadataReader _md;
        public SigTypeProvider(MetadataReader md) => _md = md;
        private static TypeSig Prim() => new() { Kind = "primitive" };
        private static TypeSig Wrap(TypeSig e) => new() { Kind = "wrap", Element = e };
        public TypeSig GetPrimitiveType(PrimitiveTypeCode c) => Prim();
        public TypeSig GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte raw) => NamedOf(r, r.GetTypeDefinition(h));
        public TypeSig GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte raw) => NamedOf(r, r.GetTypeReference(h));
        public TypeSig GetTypeFromSpecification(MetadataReader r, object? g, TypeSpecificationHandle h, byte raw) =>
            r.GetTypeSpecification(h).DecodeSignature(this, g);
        public TypeSig GetSZArrayType(TypeSig e) => Wrap(e);
        public TypeSig GetArrayType(TypeSig e, ArrayShape s) => Wrap(e);
        public TypeSig GetByReferenceType(TypeSig e) => Wrap(e);
        public TypeSig GetPointerType(TypeSig e) => Wrap(e);
        public TypeSig GetPinnedType(TypeSig e) => Wrap(e);
        public TypeSig GetGenericInstantiation(TypeSig g, ImmutableArray<TypeSig> a) => g with { Args = a };
        public TypeSig GetGenericMethodParameter(object? g, int i) => new() { Kind = "genericParam", GenericIndex = i, IsMethodParam = true };
        public TypeSig GetGenericTypeParameter(object? g, int i) => new() { Kind = "genericParam", GenericIndex = i, IsMethodParam = false };
        public TypeSig GetModifiedType(TypeSig m, TypeSig u, bool req) => u;
        public TypeSig GetFunctionPointerType(MethodSignature<TypeSig> s) =>
            new() { Kind = "fnptr", Args = s.ParameterTypes.Insert(0, s.ReturnType) };

        private TypeSig NamedOf(MetadataReader r, TypeDefinition t)
        {
            var (ns, full) = FullOf(r, t);
            return new() { Kind = "named", Namespace = ns, FullName = full };
        }
        private TypeSig NamedOf(MetadataReader r, TypeReference t)
        {
            var (ns, full) = FullOf(r, t);
            return new() { Kind = "named", Namespace = ns, FullName = full };
        }
    }

    private static (string ns, string full) FullOf(MetadataReader md, TypeDefinition t)
    {
        var name = md.GetString(t.Name);
        if (!t.GetDeclaringType().IsNil)
        {
            var (ns, outer) = FullOf(md, md.GetTypeDefinition(t.GetDeclaringType()));
            return (ns, outer + "+" + name);
        }
        var n = md.GetString(t.Namespace);
        return (n, string.IsNullOrEmpty(n) ? name : n + "." + name);
    }

    private static (string ns, string full) FullOf(MetadataReader md, TypeReference t)
    {
        var name = md.GetString(t.Name);
        if (t.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            var (ns, outer) = FullOf(md, md.GetTypeReference((TypeReferenceHandle)t.ResolutionScope));
            return (ns, outer + "+" + name);
        }
        var n = md.GetString(t.Namespace);
        return (n, string.IsNullOrEmpty(n) ? name : n + "." + name);
    }

    private static TypeSig NamedSig(string ns, string full) => new() { Kind = "named", Namespace = ns, FullName = full };

    private static TypeSig DeclaringTypeSig(MetadataReader md, SigTypeProvider prov, EntityHandle parent)
    {
        switch (parent.Kind)
        {
            case HandleKind.TypeDefinition:
                var (dns, dfull) = FullOf(md, md.GetTypeDefinition((TypeDefinitionHandle)parent));
                return NamedSig(dns, dfull);
            case HandleKind.TypeReference:
                var (rns, rfull) = FullOf(md, md.GetTypeReference((TypeReferenceHandle)parent));
                return NamedSig(rns, rfull);
            case HandleKind.TypeSpecification:
                return md.GetTypeSpecification((TypeSpecificationHandle)parent).DecodeSignature(prov, null);
            default:
                throw new InvalidOperationException("unresolved declaring parent kind " + parent.Kind);
        }
    }

    // structured supertype sig (base/interface) if its root type is in this assembly, else null
    private static TypeSig? SuperSig(MetadataReader md, SigTypeProvider prov, EntityHandle h, HashSet<string> thisAsm)
    {
        if (h.IsNil) return null;
        TypeSig sig;
        switch (h.Kind)
        {
            case HandleKind.TypeDefinition:
                var (dn, df) = FullOf(md, md.GetTypeDefinition((TypeDefinitionHandle)h));
                sig = NamedSig(dn, df);
                break;
            case HandleKind.TypeReference:
                var (rn, rf) = FullOf(md, md.GetTypeReference((TypeReferenceHandle)h));
                sig = NamedSig(rn, rf);
                break;
            case HandleKind.TypeSpecification:
                sig = md.GetTypeSpecification((TypeSpecificationHandle)h).DecodeSignature(prov, null);
                break;
            default:
                throw new InvalidOperationException("unexpected super handle " + h.Kind);
        }
        var root = NamedNodes(sig).FirstOrDefault();
        return root.FullName != null && thisAsm.Contains(root.FullName) ? sig : null;
    }

    private static readonly Dictionary<int, (int size, bool isTok)> OperandTable = BuildOperandTable();

    private static Dictionary<int, (int, bool)> BuildOperandTable()
    {
        var map = new Dictionary<int, (int, bool)>();
        foreach (var f in typeof(OpCodes).GetFields())
        {
            if (f.GetValue(null) is not OpCode op) continue;
            int val = (ushort)op.Value;
            int size = op.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.ShortInlineR => 4,
                OperandType.InlineSwitch => -1,
                _ => 4,
            };
            bool isTok = op.OperandType is OperandType.InlineField or OperandType.InlineMethod
                or OperandType.InlineType or OperandType.InlineTok;
            map[val] = (size, isTok);
        }
        return map;
    }

    private static IEnumerable<(int op, int token, bool hasTok)> Instructions(byte[] il)
    {
        int i = 0;
        while (i < il.Length)
        {
            int op = il[i]; i++;
            if (op == 0xFE)
            {
                if (i >= il.Length) throw new InvalidOperationException("truncated 2-byte opcode");
                op = 0xFE00 | il[i]; i++;
            }
            if (!OperandTable.TryGetValue(op, out var info))
                throw new InvalidOperationException($"unknown opcode 0x{op:X}");
            if (info.size == -1)
            {
                if (i + 4 > il.Length) throw new InvalidOperationException("truncated switch");
                int n = BitConverter.ToInt32(il, i);
                yield return (op, 0, false);
                i += 4 + 4 * n;
                continue;
            }
            int tok = 0;
            if (info.isTok)
            {
                if (i + 4 > il.Length) throw new InvalidOperationException("truncated token operand");
                tok = BitConverter.ToInt32(il, i);
            }
            yield return (op, tok, info.isTok);
            i += info.size;
        }
    }

    private static bool IsCallEdge(int op) => op is 0x28 or 0x6F or 0x73 or 0xFE06 or 0xFE07;
    private static bool IsVirtualEdge(int op) => op is 0x6F or 0xFE07;
    private static bool IsCalli(int op) => op == 0x29;
    private static bool IsFieldOp(int op) => op is 0x7B or 0x7C or 0x7D or 0x7E or 0x7F or 0x80;
    private static bool IsTypeOp(int op) => op is 0x74 or 0x75 or 0x8C or 0x79 or 0x8A or 0x71 or 0x81 or 0x70 or 0x8D or 0xD0 or 0xFE15 or 0xFE16 or 0xFE1C or 0xC6 or 0xC2 or 0xA3 or 0xA4 or 0xA5;

    internal static IReadOnlyList<Finding> ReachableForbidden(
        string assemblyPath, string ownerTypeFullName, IEnumerable<string> seedMethodNames,
        Func<Named, bool> isForbiddenBase, IEnumerable<(string type, string method)>? extraSignatureTargets = null)
    {
        using var fs = File.OpenRead(assemblyPath);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        var prov = new SigTypeProvider(md);

        // ---- PASS 1: complete in-assembly indexes (no cross-type resolution) ----
        var byName = new Dictionary<(string, string), List<(MethodDefinitionHandle h, ImmutableArray<TypeSig> ps)>>();
        var thisAssemblyTypes = new HashSet<string>();
        var isInterface = new HashSet<string>();
        var declaredFields = new Dictionary<string, List<TypeSig>>();
        foreach (var th in md.TypeDefinitions)
        {
            var td = md.GetTypeDefinition(th);
            var (_, tn) = FullOf(md, td);
            thisAssemblyTypes.Add(tn);
            if ((td.Attributes & TypeAttributes.Interface) != 0) isInterface.Add(tn);
            foreach (var mh in td.GetMethods())
            {
                var mdef = md.GetMethodDefinition(mh);
                var key = (tn, md.GetString(mdef.Name));
                if (!byName.TryGetValue(key, out var lst)) byName[key] = lst = new();
                lst.Add((mh, mdef.DecodeSignature(prov, null).ParameterTypes));
            }
            var fset = new List<TypeSig>();
            foreach (var fh in td.GetFields())
                fset.Add(md.GetFieldDefinition(fh).DecodeSignature(prov, null));
            declaredFields[tn] = fset;
        }

        // ---- PASS 2: resolve supertypes (structured) + MethodImpl using the complete indexes ----
        var structSupers = new Dictionary<string, List<TypeSig>>();
        var directSuperNames = new Dictionary<string, List<string>>();
        var methodImplBodies = new Dictionary<(string, string, string), List<MethodDefinitionHandle>>();
        foreach (var th in md.TypeDefinitions)
        {
            var td = md.GetTypeDefinition(th);
            var (_, tn) = FullOf(md, td);
            var sups = new List<TypeSig>();
            var b = SuperSig(md, prov, td.BaseType, thisAssemblyTypes);
            if (b != null) sups.Add(b);
            foreach (var iih in td.GetInterfaceImplementations())
            {
                var s = SuperSig(md, prov, md.GetInterfaceImplementation(iih).Interface, thisAssemblyTypes);
                if (s != null) sups.Add(s);
            }
            structSupers[tn] = sups;
            directSuperNames[tn] = sups.Select(s => NamedNodes(s).First().FullName).ToList();
            foreach (var mih in td.GetMethodImplementations())
            {
                var mi = md.GetMethodImplementation(mih);
                if (mi.MethodBody.Kind != HandleKind.MethodDefinition) continue;
                var (dt, nm, pr) = DeclKey(md, prov, mi.MethodDeclaration);
                if (dt == null) continue;
                var k = (dt, nm, pr);
                if (!methodImplBodies.TryGetValue(k, out var bl)) methodImplBodies[k] = bl = new();
                bl.Add((MethodDefinitionHandle)mi.MethodBody);
            }
        }

        var supCache = new Dictionary<string, HashSet<string>>();
        HashSet<string> Supers(string t)
        {
            if (supCache.TryGetValue(t, out var c)) return c;
            var acc = new HashSet<string>();
            var stack = new Stack<string>();
            foreach (var s in directSuperNames.GetValueOrDefault(t, new())) stack.Push(s);
            while (stack.Count > 0)
            {
                var s = stack.Pop();
                if (!acc.Add(s)) continue;
                foreach (var s2 in directSuperNames.GetValueOrDefault(s, new())) stack.Push(s2);
            }
            supCache[t] = acc;
            return acc;
        }
        var implementers = new Dictionary<string, List<string>>();
        foreach (var d in thisAssemblyTypes)
            foreach (var s in Supers(d))
            {
                if (!implementers.TryGetValue(s, out var l)) implementers[s] = l = new();
                l.Add(d);
            }

        var carriers = ComputeCarriers(declaredFields, structSupers, isForbiddenBase);
        bool ForbiddenNamed(Named n) => isForbiddenBase(n) || carriers.Contains(n.FullName);
        bool ForbiddenType(TypeSig? t) => NamedNodes(t).Any(ForbiddenNamed);
        bool BaseType(TypeSig? t) => NamedNodes(t).Any(isForbiddenBase);

        MethodDefinitionHandle Seed(string s)
        {
            if (!byName.TryGetValue((ownerTypeFullName, s), out var lst) || lst.Count == 0)
                throw new InvalidOperationException("seed method not found: " + ownerTypeFullName + "." + s);
            if (lst.Count > 1) throw new InvalidOperationException("ambiguous seed method: " + ownerTypeFullName + "." + s);
            return lst[0].h;
        }

        var findings = new List<Finding>();
        var seen = new HashSet<MethodDefinitionHandle>();
        var work = new Stack<MethodDefinitionHandle>();
        foreach (var s in seedMethodNames) work.Push(Seed(s));

        foreach (var (ty, me) in extraSignatureTargets ?? Enumerable.Empty<(string, string)>())
        {
            if (!byName.TryGetValue((ty, me), out var lst) || lst.Count == 0)
                throw new InvalidOperationException("reviewed sink not found: " + ty + "." + me);
            foreach (var (h, _) in lst) CheckContract(md, prov, h, ForbiddenType, findings);
        }

        void PushDispatch(string declType, string name, MethodSignature<TypeSig>? sig, MethodDefinitionHandle? direct)
        {
            var pr = sig is { } s ? Render(s.ParameterTypes) : null;
            var pushed = new HashSet<MethodDefinitionHandle>();
            void add(MethodDefinitionHandle h) { if (pushed.Add(h)) work.Push(h); }
            if (direct is { } d) add(d);
            var cands = new List<string>(implementers.GetValueOrDefault(declType, new())) { declType };
            foreach (var dty in cands)
                if (byName.TryGetValue((dty, name), out var lst))
                    foreach (var (h, ps) in lst)
                        if (pr == null || Render(ps) == pr)
                            add(h);
            if (pr != null && methodImplBodies.TryGetValue((declType, name, pr), out var bodies))
                foreach (var b in bodies) add(b);
        }

        while (work.Count > 0)
        {
            var mh = work.Pop();
            if (!seen.Add(mh)) continue;
            var mdef = md.GetMethodDefinition(mh);
            var (_, declFull) = FullOf(md, md.GetTypeDefinition(mdef.GetDeclaringType()));
            var methName = declFull + "." + md.GetString(mdef.Name);

            CheckContract(md, prov, mh, ForbiddenType, findings);

            if (mdef.RelativeVirtualAddress == 0) continue;
            var il = pe.GetMethodBody(mdef.RelativeVirtualAddress).GetILBytes();
            if (il == null) continue;

            foreach (var (op, token, hasTok) in Instructions(il))
            {
                if (IsCalli(op))
                    throw new InvalidOperationException("calli (indirect) call cannot be resolved statically in " + methName);
                if (!hasTok) continue;
                var handle = MetadataTokens.EntityHandle(token);

                if (IsFieldOp(op))
                {
                    var ft = FieldType(md, prov, handle);
                    if (ForbiddenType(ft))
                        findings.Add(new Finding(methName, Render(ft), "access to a field carrying a forbidden authority type"));
                    else
                    {
                        // R7-3: a field on a generic declaring type whose TYPE ARGUMENTS are forbidden
                        // (e.g. NeutralHolder<EffortResultSnapshot>.Current). Only the declaring type's
                        // generic arguments are inspected — a plain carrier holder (the orchestrator
                        // reading its own field) is NOT a data leak and is excluded.
                        var fdecl = FieldDeclaringSig(md, prov, handle);
                        if (fdecl != null && ArgNodes(fdecl).Any(ForbiddenNamed))
                            findings.Add(new Finding(methName, Render(fdecl), "field access on a generic declaring type with a forbidden/carrier type argument"));
                    }
                    continue;
                }
                if (IsCallEdge(op))
                {
                    var (declT, sig, intra, genArgs, tname) = ResolveCall(md, prov, handle, byName, thisAssemblyTypes);
                    bool newobj = op == 0x73;
                    if (newobj)
                    {
                        if (ForbiddenType(declT))
                            findings.Add(new Finding(methName, Render(declT), "construction of a forbidden/carrier authority type"));
                    }
                    else if (BaseType(declT))
                        findings.Add(new Finding(methName, Render(declT), "call/delegate to a forbidden authority type"));
                    foreach (var ga in genArgs)
                        if (ForbiddenType(ga))
                            findings.Add(new Finding(methName, Render(ga), "generic instantiation over a forbidden/carrier authority type"));
                    if (sig is { } s2)
                    {
                        if (ForbiddenType(s2.ReturnType)) findings.Add(new Finding(methName, Render(s2.ReturnType), "call returns a forbidden authority type"));
                        foreach (var p in s2.ParameterTypes)
                            if (ForbiddenType(p)) findings.Add(new Finding(methName, Render(p), "call passes a forbidden authority type"));
                    }
                    if (!newobj && IsVirtualEdge(op) && tname != null && thisAssemblyTypes.Contains(tname)
                        && IsVirtualDispatch(md, isInterface, intra, tname))
                        PushDispatch(tname, NameOf(md, handle), sig, intra);
                    else if (intra is { } t)
                        work.Push(t);
                    continue;
                }
                if (IsTypeOp(op))
                {
                    var t = TypeOperand(md, prov, handle);
                    if (ForbiddenType(t))
                        findings.Add(new Finding(methName, Render(t), "reference to a forbidden authority type"));
                    continue;
                }
            }
        }
        return findings;
    }

    private static bool IsVirtualDispatch(MetadataReader md, HashSet<string> isInterface, MethodDefinitionHandle? intra, string declType)
    {
        if (isInterface.Contains(declType)) return true;
        if (intra is { } h)
        {
            var a = md.GetMethodDefinition(h).Attributes;
            if ((a & MethodAttributes.Abstract) != 0) return true;
            if ((a & MethodAttributes.Virtual) != 0 && (a & MethodAttributes.Final) == 0) return true;
        }
        return false;
    }

    private static string NameOf(MetadataReader md, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.MethodDefinition => md.GetString(md.GetMethodDefinition((MethodDefinitionHandle)handle).Name),
        HandleKind.MemberReference => md.GetString(md.GetMemberReference((MemberReferenceHandle)handle).Name),
        HandleKind.MethodSpecification => NameOf(md, md.GetMethodSpecification((MethodSpecificationHandle)handle).Method),
        _ => throw new InvalidOperationException("no name for handle kind " + handle.Kind),
    };

    private static (string? declType, string name, string paramRender) DeclKey(MetadataReader md, SigTypeProvider prov, EntityHandle decl)
    {
        switch (decl.Kind)
        {
            case HandleKind.MethodDefinition:
                var dm = md.GetMethodDefinition((MethodDefinitionHandle)decl);
                var (_, dt) = FullOf(md, md.GetTypeDefinition(dm.GetDeclaringType()));
                return (dt, md.GetString(dm.Name), Render(dm.DecodeSignature(prov, null).ParameterTypes));
            case HandleKind.MemberReference:
                var mr = md.GetMemberReference((MemberReferenceHandle)decl);
                var root = NamedNodes(DeclaringTypeSig(md, prov, mr.Parent)).FirstOrDefault();
                string pr;
                try { pr = Render(mr.DecodeMethodSignature(prov, null).ParameterTypes); } catch { pr = ""; }
                return (root.FullName, md.GetString(mr.Name), pr);
            default:
                return (null, "", "");
        }
    }

    private static void CheckContract(MetadataReader md, SigTypeProvider prov, MethodDefinitionHandle mh,
        Func<TypeSig?, bool> forbidden, List<Finding> findings)
    {
        var mdef = md.GetMethodDefinition(mh);
        var (_, declFull) = FullOf(md, md.GetTypeDefinition(mdef.GetDeclaringType()));
        var name = declFull + "." + md.GetString(mdef.Name);
        var sig = mdef.DecodeSignature(prov, null);
        if (forbidden(sig.ReturnType)) findings.Add(new Finding(name, Render(sig.ReturnType), "method returns a forbidden authority type"));
        foreach (var p in sig.ParameterTypes)
            if (forbidden(p)) findings.Add(new Finding(name, Render(p), "method parameter is a forbidden authority type"));
    }

    private static (TypeSig decl, MethodSignature<TypeSig>? sig, MethodDefinitionHandle? intra, IReadOnlyList<TypeSig> genArgs, string? declTypeName)
        ResolveCall(MetadataReader md, SigTypeProvider prov, EntityHandle handle,
            Dictionary<(string, string), List<(MethodDefinitionHandle h, ImmutableArray<TypeSig> ps)>> byName,
            HashSet<string> thisAssemblyTypes)
    {
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                var mdh = (MethodDefinitionHandle)handle;
                var mdef = md.GetMethodDefinition(mdh);
                var (dns, dfull) = FullOf(md, md.GetTypeDefinition(mdef.GetDeclaringType()));
                return (NamedSig(dns, dfull), mdef.DecodeSignature(prov, null), mdh, Array.Empty<TypeSig>(), dfull);
            }
            case HandleKind.MethodSpecification:
            {
                var spec = md.GetMethodSpecification((MethodSpecificationHandle)handle);
                var args = spec.DecodeSignature(prov, null);
                var (decl, sig, intra, _, dtn) = ResolveCall(md, prov, spec.Method, byName, thisAssemblyTypes);
                return (decl, sig, intra, args, dtn);
            }
            case HandleKind.MemberReference:
            {
                var mr = md.GetMemberReference((MemberReferenceHandle)handle);
                var name = md.GetString(mr.Name);
                var decl = DeclaringTypeSig(md, prov, mr.Parent);
                MethodSignature<TypeSig>? sig = null;
                try { sig = mr.DecodeMethodSignature(prov, null); } catch { sig = null; }
                var rootNamed = NamedNodes(decl).FirstOrDefault();
                bool intraDecl = rootNamed.FullName != null && thisAssemblyTypes.Contains(rootNamed.FullName);
                if (!intraDecl)
                    return (decl, sig, null, Array.Empty<TypeSig>(), rootNamed.FullName);
                var target = ResolveInternal(byName, rootNamed.FullName!, name, sig);
                return (decl, sig, target, Array.Empty<TypeSig>(), rootNamed.FullName);
            }
            default:
                throw new InvalidOperationException("unresolved call target kind " + handle.Kind);
        }
    }

    private static MethodDefinitionHandle ResolveInternal(
        Dictionary<(string, string), List<(MethodDefinitionHandle h, ImmutableArray<TypeSig> ps)>> byName,
        string typeFull, string name, MethodSignature<TypeSig>? sig)
    {
        if (!byName.TryGetValue((typeFull, name), out var cands) || cands.Count == 0)
            throw new InvalidOperationException($"unresolved internal call target: {typeFull}.{name}");
        if (cands.Count == 1) return cands[0].h;
        if (sig is not { } s)
            throw new InvalidOperationException($"ambiguous internal overload with no signature: {typeFull}.{name}");
        var want = Render(s.ParameterTypes);
        var matches = cands.Where(c => Render(c.ps) == want).ToList();
        if (matches.Count == 1) return matches[0].h;
        throw new InvalidOperationException($"ambiguous/unresolved internal overload: {typeFull}.{name} ({matches.Count} matches)");
    }

    private static TypeSig? FieldType(MetadataReader md, SigTypeProvider prov, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.FieldDefinition => md.GetFieldDefinition((FieldDefinitionHandle)handle).DecodeSignature(prov, null),
        HandleKind.MemberReference => md.GetMemberReference((MemberReferenceHandle)handle).DecodeFieldSignature(prov, null),
        _ => throw new InvalidOperationException("unresolved field handle kind " + handle.Kind),
    };

    private static TypeSig? FieldDeclaringSig(MetadataReader md, SigTypeProvider prov, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.FieldDefinition:
                var (ns, full) = FullOf(md, md.GetTypeDefinition(md.GetFieldDefinition((FieldDefinitionHandle)handle).GetDeclaringType()));
                return NamedSig(ns, full);
            case HandleKind.MemberReference:
                return DeclaringTypeSig(md, prov, md.GetMemberReference((MemberReferenceHandle)handle).Parent);
            default:
                return null;
        }
    }

    private static TypeSig? TypeOperand(MetadataReader md, SigTypeProvider prov, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                var (dns, dfull) = FullOf(md, md.GetTypeDefinition((TypeDefinitionHandle)handle));
                return NamedSig(dns, dfull);
            case HandleKind.TypeReference:
                var (rns, rfull) = FullOf(md, md.GetTypeReference((TypeReferenceHandle)handle));
                return NamedSig(rns, rfull);
            case HandleKind.TypeSpecification:
                return md.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(prov, null);
            case HandleKind.FieldDefinition:
                return FieldType(md, prov, handle);
            case HandleKind.MemberReference when md.GetMemberReference((MemberReferenceHandle)handle).GetKind() == MemberReferenceKind.Field:
                return FieldType(md, prov, handle);
            case HandleKind.MethodDefinition:
                var (mns, mfull) = FullOf(md, md.GetTypeDefinition(md.GetMethodDefinition((MethodDefinitionHandle)handle).GetDeclaringType()));
                return NamedSig(mns, mfull);
            case HandleKind.MemberReference:
                return DeclaringTypeSig(md, prov, md.GetMemberReference((MemberReferenceHandle)handle).Parent);
            default:
                throw new InvalidOperationException("unresolved type operand kind " + handle.Kind);
        }
    }

    private static HashSet<string> ComputeCarriers(Dictionary<string, List<TypeSig>> declared,
        Dictionary<string, List<TypeSig>> structSupers, Func<Named, bool> isForbiddenBase)
    {
        // resolved fields per concrete type = own fields + inherited fields with generic substitution
        List<TypeSig> Collect(TypeSig named, HashSet<string> visited)
        {
            var res = new List<TypeSig>();
            var key = Render(named);
            if (!visited.Add(key)) return res;
            var name = NamedNodes(named).FirstOrDefault().FullName;
            if (name == null) return res;
            var args = named.Args;
            if (declared.TryGetValue(name, out var fs))
                foreach (var f in fs) res.Add(Subst(f, args)!);
            if (structSupers.TryGetValue(name, out var sups))
                foreach (var s in sups)
                    res.AddRange(Collect((Subst(s, args) as TypeSig)!, visited));
            return res;
        }
        var resolved = new Dictionary<string, List<TypeSig>>();
        foreach (var t in declared.Keys)
            resolved[t] = Collect(NamedSig("", t) with { FullName = t }, new HashSet<string>());

        var carriers = new HashSet<string>();
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var (tn, fts) in resolved)
            {
                if (carriers.Contains(tn)) continue;
                if (fts.Any(ft => NamedNodes(ft).Any(n => isForbiddenBase(n) || carriers.Contains(n.FullName))))
                {
                    carriers.Add(tn);
                    changed = true;
                }
            }
        }
        return carriers;
    }

    private static string Render(ImmutableArray<TypeSig> ps) => string.Join(",", ps.Select(Render));

    private static string Render(TypeSig? t)
    {
        if (t is null) return "<null>";
        return t.Kind switch
        {
            "named" => t.Args.IsEmpty ? (t.FullName ?? "?") : t.FullName + "<" + string.Join(",", t.Args.Select(Render)) + ">",
            "wrap" => Render(t.Element) + "[]",
            "primitive" => "primitive",
            "genericParam" => (t.IsMethodParam ? "!!" : "!") + t.GenericIndex,
            "fnptr" => "fnptr(" + string.Join(",", t.Args.Select(Render)) + ")",
            _ => "?",
        };
    }
}
