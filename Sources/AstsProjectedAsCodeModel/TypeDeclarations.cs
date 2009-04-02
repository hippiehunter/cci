//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.Contracts;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  public class GlobalDeclarationContainerClass : NamespaceClassDeclaration {

    public GlobalDeclarationContainerClass(IMetadataHost compilationHost)
      : this(compilationHost, new List<ITypeDeclarationMember>()) {
    }

    private GlobalDeclarationContainerClass(IMetadataHost compilationHost, List<ITypeDeclarationMember> globalMembers)
      : base(null, Flags.None, new NameDeclaration(compilationHost.NameTable.GetNameFor("__Globals__"), SourceDummy.SourceLocation),
      new List<GenericTypeParameterDeclaration>(0), new List<TypeExpression>(0), globalMembers, SourceDummy.SourceLocation) {
      this.globalMembers = globalMembers;
    }

    protected GlobalDeclarationContainerClass(NamespaceDeclaration containingNamespaceDeclaration, GlobalDeclarationContainerClass template)
      : this(containingNamespaceDeclaration, template, template.globalMembers)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
    }

    protected GlobalDeclarationContainerClass(NamespaceDeclaration containingNamespaceDeclaration, GlobalDeclarationContainerClass template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
      this.globalMembers = members;
    }


    protected internal override NamespaceTypeDefinition CreateType() {
      NamespaceTypeDefinition result = this.Compilation.GlobalsClass;
      result.AddTypeDeclaration(this);
      ITypeContract/*?*/ typeContract = this.Compilation.ContractProvider.GetTypeContractFor(this);
      if (typeContract != null)
        this.Compilation.ContractProvider.AssociateTypeWithContract(result, typeContract);
      return result;
    }

    public IEnumerable<ITypeDeclarationMember> GlobalMembers {
      get
        //^ ensures result is List<ITypeDeclarationMember>; //The return type is different so that a downcast is required before the members can be modified.
        //TODO: make the post condition valid only while the class has not yet been fully initialized.
      { 
        return this.globalMembers; 
      }
    }
    List<ITypeDeclarationMember> globalMembers;

    public IScope<ITypeDeclarationMember> GlobalScope {
      get {
        if (this.globalScope == null)
          this.globalScope = new GlobalDeclarationScope(this.GlobalMembers);
        return this.globalScope; 
      }
    }
    //^ [Once]
    IScope<ITypeDeclarationMember>/*?*/ globalScope;

    //^ [MustOverride]
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)      
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new GlobalDeclarationContainerClass(targetNamespaceDeclaration, this);
    }

    //^ [MustOverride]
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new GlobalDeclarationContainerClass(this.ContainingNamespaceDeclaration, this, members);
    }

  }

  internal class GlobalDeclarationScope : Scope<ITypeDeclarationMember> {

    internal GlobalDeclarationScope(IEnumerable<ITypeDeclarationMember> members) {
      this.members = members;
    }

    protected override void InitializeIfNecessary() {
      if (this.initialized) return;
      lock (GlobalLock.LockingObject) {
        if (this.initialized) return;
        foreach (ITypeDeclarationMember member in members)
          this.AddMemberToCache(member);
        this.initialized = true;
      }
    }
    bool initialized;

    public override IEnumerable<ITypeDeclarationMember> Members {
      get { return this.Members; }
    }
    IEnumerable<ITypeDeclarationMember> members;
  }

  /// <summary>
  /// Corresponds to a source construct that declares a type parameter for a generic method or type.
  /// </summary>
  public abstract class GenericParameterDeclaration : SourceItem, IDeclaration, INamedEntity, IParameterListEntry {

    protected GenericParameterDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, NameDeclaration name, ushort index, List<TypeExpression> constraints, 
      TypeParameterVariance variance, bool mustBeReferenceType, bool mustBeValueType, bool mustHaveDefaultConstructor, ISourceLocation sourceLocation)
      : base(sourceLocation) 
      //^ requires !mustBeReferenceType || !mustBeValueType;
    {
      this.constraints = constraints;
      this.flags = (int)variance;
      if (mustBeReferenceType) this.flags |= 0x40000000;
      if (mustBeValueType) this.flags |= 0x20000000;
      if (mustHaveDefaultConstructor) this.flags |= 0x10000000;
      this.index = index;
      this.name = name;
      this.sourceAttributes = sourceAttributes;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected GenericParameterDeclaration(BlockStatement containingBlock, GenericParameterDeclaration template)
      : base(template.SourceLocation) {
      this.containingBlock = containingBlock;
      this.constraints = new List<TypeExpression>(template.constraints);
      this.flags = template.flags;
      this.index = template.index;
      this.name = template.name;
      if (template.sourceAttributes != null)
        this.sourceAttributes = new List<SourceCustomAttribute>(template.sourceAttributes);
    }

    public IEnumerable<ICustomAttribute> Attributes {
      get {
        return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); //TODO: extract attributes from SourceAttributes.
      }
    }

    protected void AddConstraint(TypeExpression constraint) {
      this.constraints.Add(constraint);
    }

    /// <summary>
    /// A list of classes or interfaces. All type arguments matching this parameter must be derived from all of the classes and implement all of the interfaces.
    /// </summary>
    public IEnumerable<TypeExpression> Constraints {
      get {
        for (int i = 0, n = this.constraints.Count; i < n; i++)
          yield return this.constraints[i] = (TypeExpression)this.constraints[i].MakeCopyFor(this.ContainingBlock);
      }
    }
    readonly List<TypeExpression> constraints;

    public Compilation Compilation {
      get { return this.ContainingBlock.Compilation;  }
    }

    public CompilationPart CompilationPart {
      get { return this.ContainingBlock.CompilationPart; }
    }

    public BlockStatement ContainingBlock {
      get { 
        //^ assume this.containingBlock != null;
        return this.containingBlock; 
      }
    }
    protected BlockStatement/*?*/ containingBlock;

    private int flags;

    public ushort Index {
      get { return this.index; }
    }
    readonly ushort index;

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be reference types.
    /// </summary>
    public bool MustBeReferenceType {
      get
        //^ ensures result ==> !this.MustBeValueType;
        //^ ensures result == ((this.flags & 0x60000000) == 0x40000000);
      { 
        bool result = (this.flags & 0x60000000) == 0x40000000; 
        //^ assume result ==> !this.MustBeValueType;
        return result;
      }
      protected set {
        this.flags &= ~0x60000000;
        if (value) this.flags |= 0x40000000;
      }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types.
    /// </summary>
    public bool MustBeValueType {
      get
        //^ ensures result == ((this.flags & 0x60000000) == 0x20000000);
      { 
        bool result = (this.flags & 0x60000000) == 0x20000000;
        //^ assume result ==> !this.MustBeReferenceType;
        return result;
      }
      protected set {
        this.flags &= ~0x60000000;
        if (value) this.flags |= 0x20000000;
      }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types or concrete classes with visible default constructors.
    /// </summary>
    public bool MustHaveDefaultConstructor {
      get { return (this.flags & 0x10000000) != 0; }
      protected set { if (value) this.flags |= 0x10000000; else this.flags &= ~0x10000000; }
    }

    public NameDeclaration Name {
      get { return this.name; }
    }
    readonly NameDeclaration name;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a generic parameter before constructing the declaring type or method.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingExpression(Expression containingExpression) {
      this.containingBlock = containingExpression.ContainingBlock;
      foreach (TypeExpression constraint in this.constraints) constraint.SetContainingExpression(containingExpression);
      if (this.sourceAttributes != null)
        foreach (SourceCustomAttribute attribute in this.sourceAttributes) attribute.SetContainingExpression(containingExpression);
    }

    public IEnumerable<SourceCustomAttribute> SourceAttributes {
      get {
        List<SourceCustomAttribute> sourceAttributes;
        if (this.sourceAttributes == null)
          yield break;
        else
          sourceAttributes = this.sourceAttributes;
        for (int i = 0, n = sourceAttributes.Count; i < n; i++) {
          yield return sourceAttributes[i] = sourceAttributes[i].MakeShallowCopyFor(this.ContainingBlock);
        }
      }
    }
    readonly List<SourceCustomAttribute>/*?*/ sourceAttributes;
    //TODO: rather than use may be null fields to store parsely populated properties, use a dictionary 

    /// <summary>
    /// Indicates if the generic type or method with this type parameter is co-, contra-, or non variant with respect to this type parameter.
    /// </summary>
    public TypeParameterVariance Variance {
      get { return ((TypeParameterVariance)this.flags) & TypeParameterVariance.Mask; }
      protected set { this.flags |= (int)value; }
    }

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source construct that declares a type parameter for a generic type.
  /// </summary>
  public class GenericTypeParameterDeclaration : GenericParameterDeclaration {

    public GenericTypeParameterDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, NameDeclaration name, 
      ushort index, List<TypeExpression> constraints, TypeParameterVariance variance, bool mustBeReferenceType, bool mustBeValueType, bool mustHaveDefaultConstructor, ISourceLocation sourceLocation)
      : base(sourceAttributes, name, index, constraints, variance, mustBeReferenceType, mustBeValueType, mustHaveDefaultConstructor, sourceLocation)
      //^ requires !mustBeReferenceType || !mustBeValueType;
    {
    }

    protected GenericTypeParameterDeclaration(TypeDeclaration declaringType, GenericTypeParameterDeclaration template)
      : base(declaringType.OuterDummyBlock, template) {
      this.declaringType = declaringType;
    }

    /// <summary>
    /// Makes a copy of this generic type parameter declaration, changing the target type to the given type.
    /// </summary>
    public virtual GenericTypeParameterDeclaration MakeCopyFor(TypeDeclaration targetDeclaringType) {
      if (this.declaringType == targetDeclaringType) return this;
      return new GenericTypeParameterDeclaration(targetDeclaringType, this);
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The generic type that declares this type parameter.
    /// </summary>
    public TypeDeclaration DeclaringType {
      get { 
        //^ assume this.declaringType != null;
        return this.declaringType;
      }
    }
    //^ [SpecPublic]
    TypeDeclaration/*?*/ declaringType;

    /// <summary>
    /// The symbol table entity that corresponds to this source construct.
    /// </summary>
    public IGenericTypeParameter GenericTypeParameterDefinition {
      get {
        foreach (GenericTypeParameter genericTypeParameter in this.DeclaringType.TypeDefinition.GenericParameters)
          if (genericTypeParameter.Index == this.Index) return genericTypeParameter;
        //^ assume false; //It is not OK to create a GenericTypeParameterDeclaration whose declaring type does not know about it.
        return Dummy.GenericTypeParameter;
      }
    }

    public virtual void SetDeclaringType(TypeDeclaration declaringType) 
      //^ requires this.declaringType == null;
    {
      DummyExpression containingExpression = new DummyExpression(declaringType.OuterDummyBlock, SourceDummy.SourceLocation);
      this.SetContainingExpression(containingExpression);
      this.declaringType = declaringType;
    }

  }

  /// <summary>
  /// Corresponds to a source construct that declares a class nested directly inside a namespace.
  /// </summary>
  public class NamespaceClassDeclaration : NamespaceTypeDeclaration, IClassDeclaration, INamespaceDeclarationMember {

    public NamespaceClassDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration>/*?*/ genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation)
    {
    }

    protected NamespaceClassDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceClassDeclaration template)
      : base(containingNamespaceDeclaration, template)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
    }

    protected NamespaceClassDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceClassDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NamespaceClassDeclaration(this.ContainingNamespaceDeclaration, this, members);
    }

    //^ [MustOverride]
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new NamespaceClassDeclaration(targetNamespaceDeclaration, this);
    }

  }

  /// <summary>
  /// Corresponds to a source construct that declares a delegate nested directly inside a namespace.
  /// </summary>
  public class NamespaceDelegateDeclaration : NamespaceTypeDeclaration, IDelegateDeclaration, INamespaceDeclarationMember {

    public NamespaceDelegateDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, 
      Flags flags, NameDeclaration name, List<GenericTypeParameterDeclaration> genericParameters, SignatureDeclaration signature, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, new List<TypeExpression>(0), new List<ITypeDeclarationMember>(0), sourceLocation)
    {
      this.signature = signature;
    }

    protected NamespaceDelegateDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceDelegateDeclaration template)
      : base(containingNamespaceDeclaration, template)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
      this.signature = template.signature.MakeShallowCopyFor(containingNamespaceDeclaration.DummyBlock);
      //^ assume this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    }

    protected NamespaceDelegateDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceDelegateDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members) {
      this.signature = template.signature.MakeShallowCopyFor(containingNamespaceDeclaration.DummyBlock);
    }

    protected internal override NamespaceTypeDefinition CreateType() {
      return base.CreateType();
      //TODO: create a derived type specific to delegates
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NamespaceDelegateDeclaration(this.ContainingNamespaceDeclaration, this, members);
    }

    //^ [MustOverride]
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new NamespaceDelegateDeclaration(targetNamespaceDeclaration, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a namespace type before constructing the namespace.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, bool recurse) {
      base.SetContainingNamespaceDeclaration(containingNamespaceDeclaration, recurse);
      this.Signature.SetContainingBlock(this.DummyBlock);
    }

    public SignatureDeclaration Signature {
      get {
        return this.signature;
      }
    }
    readonly SignatureDeclaration signature;

    #region IDelegateDeclaration Members

    ISignatureDeclaration IDelegateDeclaration.Signature {
      get { 
        return this.Signature; 
      }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source construct that declares an enumerated scalar type nested directly inside a namespace.
  /// </summary>
  public class NamespaceEnumDeclaration : NamespaceTypeDeclaration, IEnumDeclaration, INamespaceDeclarationMember {

    public NamespaceEnumDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name, 
       TypeExpression/*?*/ underlyingType, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags|TypeDeclaration.Flags.Sealed, name, new List<GenericTypeParameterDeclaration>(0), new List<TypeExpression>(0), members, sourceLocation)
    {
      this.underlyingType = underlyingType;
    }

    protected NamespaceEnumDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceEnumDeclaration template)
      : base(containingNamespaceDeclaration, template) {
      if (template.underlyingType != null)
        this.underlyingType = (TypeExpression)template.underlyingType.MakeCopyFor(containingNamespaceDeclaration.DummyBlock);
    }

    protected NamespaceEnumDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceEnumDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members) {
      if (template.underlyingType != null)
        this.underlyingType = (TypeExpression)template.underlyingType.MakeCopyFor(containingNamespaceDeclaration.DummyBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(NamespaceEnumDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NamespaceEnumDeclaration(this.ContainingNamespaceDeclaration, this, members);
    }

    //^ [MustOverride]
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new NamespaceEnumDeclaration(targetNamespaceDeclaration, this);
    }

    public override void SetContainingNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, bool recurse) {
      TypeExpression/*?*/ thisType = this.UnderlyingType;
      if (thisType == null) thisType = TypeExpression.For(containingNamespaceDeclaration.Helper.PlatformType.SystemInt32.ResolvedType);
      NameDeclaration value__ = new NameDeclaration(containingNamespaceDeclaration.Helper.NameTable.GetNameFor("value__"), SourceDummy.SourceLocation);
      FieldDeclaration field = new FieldDeclaration(null, FieldDeclaration.Flags.RuntimeSpecial|FieldDeclaration.Flags.SpecialName, 
        TypeMemberVisibility.Public, thisType, value__, null, SourceDummy.SourceLocation);
      this.AddHelperMember(field); 
      base.SetContainingNamespaceDeclaration(containingNamespaceDeclaration, recurse);
      field.SetContainingTypeDeclaration(this, true);
      if (this.UnderlyingType != null)
        this.UnderlyingType.SetContainingExpression(new DummyExpression(this.DummyBlock, SourceDummy.SourceLocation));
    }

    public TypeExpression/*?*/ UnderlyingType {
      get {
        return this.underlyingType;
      }
    }
    readonly TypeExpression/*?*/ underlyingType;

    #region IEnumDeclaration Members

    TypeExpression/*?*/ IEnumDeclaration.UnderlyingType {
      get { 
        return this.UnderlyingType; 
      }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source construct that declares an interface nested directly inside a namespace.
  /// </summary>
  public class NamespaceInterfaceDeclaration : NamespaceTypeDeclaration, IInterfaceDeclaration, INamespaceDeclarationMember {

    public NamespaceInterfaceDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration>/*?*/ genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation)
    {
    }

    protected NamespaceInterfaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceInterfaceDeclaration template)
      : base(containingNamespaceDeclaration, template) {
    }

    protected NamespaceInterfaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceInterfaceDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members) {
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NamespaceInterfaceDeclaration(this.ContainingNamespaceDeclaration, this, members);
    }

    //^ [MustOverride]
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new NamespaceInterfaceDeclaration(targetNamespaceDeclaration, this);
    }

  }

  /// <summary>
  /// Corresponds to a source construct that declares a value type (struct) nested directly inside a namespace.
  /// </summary>
  public class NamespaceStructDeclaration : NamespaceTypeDeclaration, IStructDeclaration, INamespaceDeclarationMember {

    public NamespaceStructDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name, 
      List<GenericTypeParameterDeclaration> genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags|TypeDeclaration.Flags.Sealed, name, genericParameters, baseTypes, members, sourceLocation) 
    {
    }

    protected NamespaceStructDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceStructDeclaration template)
      : base(containingNamespaceDeclaration, template) {
    }

    protected NamespaceStructDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceStructDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration, template, members) {
    }

    /// <summary>
    /// Calls the visitor.Visit(NamespaceStructDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    protected override NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NamespaceStructDeclaration(this.ContainingNamespaceDeclaration, this, members);
    }

    /// <summary>
    /// Layout of the type declaration.
    /// </summary>
    public override LayoutKind Layout {
      get {
        return LayoutKind.Sequential; //TODO: get this from a custom attribute
      }
    }

    //^ [MustOverride]
    public override INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      if (targetNamespaceDeclaration == this.ContainingNamespaceDeclaration) return this;
      return new NamespaceStructDeclaration(targetNamespaceDeclaration, this);
    }

  }

  /// <summary>
  /// Corresponds to a source language type declaration (at the top level or nested in a namespace), such as a C# partial class. 
  /// One of more of these make up a type definition. 
  /// Each contains a collection of <see cref="NamespaceTypeDeclaration"/> instances in its <see cref="TypeDeclaration.TypeDeclarationMembers"/> property.
  /// The union of the collections make up the Members property of the type definition.
  /// </summary>
  public abstract class NamespaceTypeDeclaration : TypeDeclaration, INamespaceDeclarationMember, IAggregatableNamespaceDeclarationMember {

    protected NamespaceTypeDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name,
      List<GenericTypeParameterDeclaration>/*?*/ genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation)
    {
    }

    protected NamespaceTypeDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceTypeDeclaration template)
      : base(containingNamespaceDeclaration.DummyBlock, template)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
      this.containingNamespaceDeclaration = containingNamespaceDeclaration;
    }

    protected NamespaceTypeDeclaration(NamespaceDeclaration containingNamespaceDeclaration, NamespaceTypeDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingNamespaceDeclaration.DummyBlock, template, members)
      //^ ensures this.containingNamespaceDeclaration == containingNamespaceDeclaration;
    {
      this.containingNamespaceDeclaration = containingNamespaceDeclaration;
    }

    /// <summary>
    /// The namespace that contains this member.
    /// </summary>
    public NamespaceDeclaration ContainingNamespaceDeclaration {
      get
        //^ ensures result == this.containingNamespaceDeclaration;
      {
        //^ assume this.containingNamespaceDeclaration != null;
        return this.containingNamespaceDeclaration;
      }
    }
    protected NamespaceDeclaration/*?*/ containingNamespaceDeclaration;

    public override BlockStatement DummyBlock {
      get {
        if (this.dummyBlock == null) {
          BlockStatement dummyBlock = new BlockStatement(new List<Statement>(0), this.SourceLocation);
          dummyBlock.SetContainers(this.ContainingNamespaceDeclaration.DummyBlock, this);
          lock (this) {
            if (this.dummyBlock == null) {
              this.dummyBlock = dummyBlock;
            }
          }
        }
        return this.dummyBlock;
      }
    }
    //^ [Once]
    private BlockStatement/*?*/ dummyBlock;

    private NamespaceTypeDefinition GetOrCreateType() {
      foreach (INamespaceMember member in this.ContainingNamespaceDeclaration.UnitNamespace.GetMembersNamed(this.Name, false)) {
        NamespaceTypeDefinition/*?*/ nt = member as NamespaceTypeDefinition;
        if (nt != null && nt.GenericParameterCount == this.GenericParameterCount) {
          if (this.namespaceTypeDefinition == nt) return nt;
          nt.AddTypeDeclaration(this);
          return nt;
        }
      }
      return this.CreateType();
    }

    protected internal virtual NamespaceTypeDefinition CreateType() {
      NamespaceTypeDefinition result = new NamespaceTypeDefinition(this.ContainingNamespaceDeclaration.UnitNamespace, this.Name, this.Compilation.HostEnvironment.InternFactory);
      this.namespaceTypeDefinition = result;
      result.AddTypeDeclaration(this);
      ITypeContract/*?*/ typeContract = this.Compilation.ContractProvider.GetTypeContractFor(this);
      if (typeContract != null)
        this.Compilation.ContractProvider.AssociateTypeWithContract(result, typeContract);
      return result;
    }

    /// <summary>
    /// If true, this type is accessible outside of the unit that contains it.
    /// </summary>
    public virtual bool IsPublic {
      get {
        return (((TypeMemberVisibility)this.flags) & TypeMemberVisibility.Mask) == TypeMemberVisibility.Public;
      }
    }

    protected abstract NamespaceTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit);
    //^ ensures result.GetType() == this.GetType();

    public abstract INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration);
    //^ ensures result.GetType() == this.GetType();
    //^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;

    /// <summary>
    /// The symbol table entity that corresponds to this source construct.
    /// </summary>
    public NamespaceTypeDefinition NamespaceTypeDefinition {
      get {
        if (this.namespaceTypeDefinition == null)
          this.namespaceTypeDefinition = this.GetOrCreateType();
        return this.namespaceTypeDefinition;
      }
    }
    NamespaceTypeDefinition/*?*/ namespaceTypeDefinition;

    public override BlockStatement OuterDummyBlock {
      get {
        BlockStatement/*?*/ outerDummyBlock = this.outerDummyBlock;
        if (outerDummyBlock == null) {
          lock (GlobalLock.LockingObject) {
            if (this.outerDummyBlock == null) {
              this.outerDummyBlock = outerDummyBlock = new BlockStatement(new List<Statement>(0), this.SourceLocation);
              outerDummyBlock.SetContainers(this.ContainingNamespaceDeclaration.DummyBlock, this);
            }
          }
        }
        return outerDummyBlock;
      }
    }
    BlockStatement/*?*/ outerDummyBlock;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a namespace type before constructing the namespace.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingNamespaceDeclaration(NamespaceDeclaration containingNamespaceDeclaration, bool recurse) {
      this.containingNamespaceDeclaration = containingNamespaceDeclaration;
      this.OuterDummyBlock.SetContainers(containingNamespaceDeclaration.DummyBlock, this);
      this.SetCompilationPart(containingNamespaceDeclaration.CompilationPart, recurse);
    }

    public override TypeDefinition TypeDefinition {
      get { return this.NamespaceTypeDefinition; }
    }

    public override TypeDeclaration UpdateMembers(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDcoument);
      //^^ ensures result.GetType() == this.GetType();
    {
      NamespaceTypeDeclaration result = this.MakeShallowCopy(members, edit);
      ISourceDocument afterEdit = edit.SourceDocumentAfterEdit;
      ISourceLocation locationBeforeEdit = this.SourceLocation;
      //^ assume afterEdit.IsUpdatedVersionOf(locationBeforeEdit.SourceDocument);
      result.sourceLocation = afterEdit.GetCorrespondingSourceLocation(locationBeforeEdit);
      List<INamespaceDeclarationMember> newParentMembers = new List<INamespaceDeclarationMember>(this.ContainingNamespaceDeclaration.Members);
      NamespaceDeclaration newParent = this.ContainingNamespaceDeclaration.UpdateMembers(newParentMembers, edit);
      result.containingNamespaceDeclaration = newParent;
      for (int i = 0, n = newParentMembers.Count; i < n; i++) {
        if (newParentMembers[i] == this) { newParentMembers[i] = result; break; }
      }
      return result;
    }

    #region INamespaceDeclarationMember Members

    NamespaceDeclaration INamespaceDeclarationMember.ContainingNamespaceDeclaration {
      get { return this.ContainingNamespaceDeclaration; }
    }

    INamespaceDeclarationMember INamespaceDeclarationMember.MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration)
      //^^ requires targetNamespaceDeclaration.GetType() == this.ContainingNamespaceDeclaration.GetType();
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;
    {
      //^ assume targetNamespaceDeclaration is NamespaceDeclaration; //Follows from the precondition
      return this.MakeShallowCopyFor((NamespaceDeclaration)targetNamespaceDeclaration);
    }

    #endregion

    #region IContainerMember<NamespaceDeclaration> Members

    NamespaceDeclaration IContainerMember<NamespaceDeclaration>.Container {
      get { return this.ContainingNamespaceDeclaration; }
    }

    IName IContainerMember<NamespaceDeclaration>.Name {
      get { return this.Name; }
    }

    #endregion

    #region IAggregatableNamespaceDeclarationMember Members

    INamespaceMember IAggregatableNamespaceDeclarationMember.AggregatedMember {
      get { return this.NamespaceTypeDefinition; }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source construct that declares a class nested inside another type.
  /// </summary>
  public class NestedClassDeclaration : NestedTypeDeclaration, IClassDeclaration {

    public NestedClassDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name, 
      List<GenericTypeParameterDeclaration> genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation)
    {
    }

    // Create a nested class declaration from a non nested one
    public NestedClassDeclaration(NamespaceClassDeclaration sourceClassDeclaration)
      : base(sourceClassDeclaration)
    {
    }

    protected NestedClassDeclaration(TypeDeclaration containingTypeDeclaration, NestedClassDeclaration template)
      : base(containingTypeDeclaration, template) {
    }

    protected NestedClassDeclaration(TypeDeclaration containingTypeDeclaration, NestedClassDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingTypeDeclaration, template, members) {
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    protected override NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NestedClassDeclaration(this.ContainingTypeDeclaration, this, members);
    }

    //^ [MustOverride]
    public override ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new NestedClassDeclaration(targetTypeDeclaration, this);
    }

  }

  /// <summary>
  /// Corresponds to a source construct that declares a delegate nested inside another type.
  /// </summary>
  public class NestedDelegateDeclaration : NestedTypeDeclaration, IDelegateDeclaration {

    public NestedDelegateDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name, 
      List<GenericTypeParameterDeclaration> genericParameters, SignatureDeclaration signature, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, 
      genericParameters, new List<TypeExpression>(0), new List<ITypeDeclarationMember>(0), sourceLocation)
    {
      this.signature = signature;
    }

    protected NestedDelegateDeclaration(TypeDeclaration containingTypeDeclaration, NestedDelegateDeclaration template)
      : base(containingTypeDeclaration, template) {
      this.signature = template.signature; //TODO: copy the signature
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    protected override NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NestedDelegateDeclaration(this.ContainingTypeDeclaration, this);
    }

    //^ [MustOverride]
    public override ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new NestedDelegateDeclaration(targetTypeDeclaration, this);
    }

    public override void SetContainingTypeDeclaration(TypeDeclaration containingTypeDeclaration, bool recurse) {
      base.SetContainingTypeDeclaration(containingTypeDeclaration, recurse);
      this.Signature.SetContainingBlock(this.DummyBlock);
    }

    public SignatureDeclaration Signature {
      get {
        return this.signature;
      }
    }
    readonly SignatureDeclaration signature;

    #region IDelegateDeclaration Members

    ISignatureDeclaration IDelegateDeclaration.Signature {
      get {
        return this.signature;
      }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source construct that declares an enumerated scalar type nested inside another type.
  /// </summary>
  public class NestedEnumDeclaration : NestedTypeDeclaration, IEnumDeclaration {

    public NestedEnumDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name, 
      TypeExpression/*?*/ underlyingType, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags|TypeDeclaration.Flags.Sealed, name, new List<GenericTypeParameterDeclaration>(0), new List<TypeExpression>(0), members, sourceLocation)
    {
      this.underlyingType = underlyingType;
    }

    protected NestedEnumDeclaration(TypeDeclaration containingTypeDeclaration, NestedEnumDeclaration template)
      : base(containingTypeDeclaration, template) {
      if (template.UnderlyingType != null)
        this.underlyingType = (TypeExpression)template.UnderlyingType.MakeCopyFor(containingTypeDeclaration.OuterDummyBlock);
    }

    protected NestedEnumDeclaration(TypeDeclaration containingTypeDeclaration, NestedEnumDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingTypeDeclaration, template, members) {
      if (template.UnderlyingType != null)
        this.underlyingType = (TypeExpression)template.UnderlyingType.MakeCopyFor(containingTypeDeclaration.OuterDummyBlock);
    }

    /// <summary>
    /// Calls the visitor.Visit(NestedEnumDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    protected override NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NestedEnumDeclaration(this.ContainingTypeDeclaration, this, members);
    }

    //^ [MustOverride]
    public override ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new NestedEnumDeclaration(targetTypeDeclaration, this);
    }

    public override void SetCompilationPart(CompilationPart compilationPart, bool recurse) {
      TypeExpression/*?*/ thisType = this.UnderlyingType;
      if (thisType == null) thisType = TypeExpression.For(compilationPart.Helper.PlatformType.SystemInt32.ResolvedType);
      NameDeclaration value__ = new NameDeclaration(compilationPart.Helper.NameTable.GetNameFor("value__"), SourceDummy.SourceLocation);
      FieldDeclaration field = new FieldDeclaration(null, FieldDeclaration.Flags.RuntimeSpecial|FieldDeclaration.Flags.SpecialName, 
        TypeMemberVisibility.Public, thisType, value__, null, SourceDummy.SourceLocation);
      this.AddHelperMember(field);
      base.SetCompilationPart(compilationPart, recurse);
      field.SetContainingTypeDeclaration(this, true);
      if (!recurse) return;
      BlockStatement dummyBlock = this.OuterDummyBlock;
      DummyExpression containingExpression = new DummyExpression(dummyBlock, SourceDummy.SourceLocation);
      if (this.underlyingType != null)
        this.underlyingType.SetContainingExpression(containingExpression);
    }

    public TypeExpression/*?*/ UnderlyingType {
      get {
        return this.underlyingType;
      }
    }
    readonly TypeExpression/*?*/ underlyingType;

    #region IEnumDeclaration Members

    TypeExpression/*?*/ IEnumDeclaration.UnderlyingType {
      get {
        return this.UnderlyingType;
      }
    }

    #endregion
  }

  /// <summary>
  /// Corresponds to a source construct that declares an interface nested inside another type.
  /// </summary>
  public class NestedInterfaceDeclaration : NestedTypeDeclaration, IInterfaceDeclaration {

    public NestedInterfaceDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name, 
      List<GenericTypeParameterDeclaration> genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation) 
    {
    }

    protected NestedInterfaceDeclaration(TypeDeclaration containingTypeDeclaration, NestedInterfaceDeclaration template)
      : base(containingTypeDeclaration, template) {
    }

    protected NestedInterfaceDeclaration(TypeDeclaration containingTypeDeclaration, NestedInterfaceDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingTypeDeclaration, template, members) {
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [MustOverride]
    protected override NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NestedInterfaceDeclaration(this.ContainingTypeDeclaration, this, members);
    }

    //^ [MustOverride]
    public override ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new NestedInterfaceDeclaration(targetTypeDeclaration, this);
    }

  }

  /// <summary>
  /// Corresponds to a source construct that declares a value type (struct) nested inside another type.
  /// </summary>
  public class NestedStructDeclaration : NestedTypeDeclaration, IStructDeclaration {

    public NestedStructDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name, 
      List<GenericTypeParameterDeclaration> genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags|TypeDeclaration.Flags.Sealed, name, genericParameters, baseTypes, members, sourceLocation)
    {
    }

    protected NestedStructDeclaration(TypeDeclaration containingTypeDeclaration, NestedStructDeclaration template)
      : base(containingTypeDeclaration, template)
      // ^ ensures this.ContainingTypeDeclaration == containingTypeDeclaration; //Spec# problem with delayed receiver
    {
    }

    protected NestedStructDeclaration(TypeDeclaration containingTypeDeclaration, NestedStructDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingTypeDeclaration, template, members)
      // ^ ensures this.ContainingTypeDeclaration == containingTypeDeclaration; //Spec# problem with delayed receiver
    {
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Layout of the type declaration.
    /// </summary>
    public override LayoutKind Layout {
      get {
        return LayoutKind.Sequential; //TODO: get this from a custom attribute
      }
    }

    //^ [MustOverride]
    protected override NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members)
      //^^ ensures result.GetType() == this.GetType();
    {
      return new NestedStructDeclaration(this.ContainingTypeDeclaration, this, members);
    }

    //^ [MustOverride]
    public override ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration) {
      if (targetTypeDeclaration == this.ContainingTypeDeclaration) return this;
      return new NestedStructDeclaration(targetTypeDeclaration, this);
    }
  }

  /// <summary>
  /// Corresponds to a source language type declaration nested inside another type declaration, such as a C# partial class. 
  /// One of more of these make up a type definition. 
  /// Each contains a collection of <see cref="NestedTypeDeclaration"/> instances in its <see cref="TypeDeclaration.TypeDeclarationMembers"/> property.
  /// The union of the collections make up the Members property of the type definition.
  /// </summary>
  public abstract class NestedTypeDeclaration : TypeDeclaration, ITypeDeclarationMember, IAggregatableTypeDeclarationMember {

    protected NestedTypeDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name, 
      List<GenericTypeParameterDeclaration> genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceAttributes, flags, name, genericParameters, baseTypes, members, sourceLocation) 
    {
    }

    protected NestedTypeDeclaration(NamespaceTypeDeclaration sourceTypeDeclaration)
        : base(sourceTypeDeclaration.CompilationPart, sourceTypeDeclaration)
    {
    }

    protected NestedTypeDeclaration(TypeDeclaration containingTypeDeclaration, NestedTypeDeclaration template)
      : base(containingTypeDeclaration.DummyBlock, template)
      // ^ ensures this.ContainingTypeDeclaration == containingTypeDeclaration; //Spec# problem with delayed receiver
    {
      this.containingTypeDeclaration = containingTypeDeclaration;
    }

    protected NestedTypeDeclaration(TypeDeclaration containingTypeDeclaration, NestedTypeDeclaration template, List<ITypeDeclarationMember> members)
      : base(containingTypeDeclaration.DummyBlock, template, members)
      // ^ ensures this.ContainingTypeDeclaration == containingTypeDeclaration; //Spec# problem with delayed receiver
    {
      this.containingTypeDeclaration = containingTypeDeclaration;
    }

    public TypeDeclaration ContainingTypeDeclaration {
      get {
        //^ assume this.containingTypeDeclaration != null;
        return this.containingTypeDeclaration; 
      }
    }
    //^ [SpecPublic]
    TypeDeclaration/*?*/ containingTypeDeclaration;

    public override BlockStatement DummyBlock {
      get {
        if (this.dummyBlock == null) {
          BlockStatement dummyBlock = new BlockStatement(new List<Statement>(0), this.SourceLocation);
          dummyBlock.SetContainers(this.ContainingTypeDeclaration.DummyBlock, this);
          lock (this) {
            if (this.dummyBlock == null) {
              this.dummyBlock = dummyBlock;
            }
          }
        }
        return this.dummyBlock;
      }
    }
    //^ [Once]
    private BlockStatement/*?*/ dummyBlock;

    private NestedTypeDefinition GetOrCreateNestedType() {
      foreach (TypeDeclaration containingTypeDeclaration in this.ContainingTypeDeclaration.TypeDefinition.TypeDeclarations) {
        foreach (ITypeDeclarationMember member in containingTypeDeclaration.TypeDeclarationMembers) {
          if (member == this) continue;
          NestedTypeDeclaration/*?*/ nt = member as NestedTypeDeclaration;
          if (nt != null && nt.Name.UniqueKey == this.Name.UniqueKey && nt.GenericParameterCount == this.GenericParameterCount) {
            nt.TypeDefinition.AddTypeDeclaration(this);
            return nt.NestedTypeDefinition;
          }
        }
      }
      return this.CreateNestedType();
    }

    protected internal virtual NestedTypeDefinition CreateNestedType() {
      NestedTypeDefinition result = new NestedTypeDefinition(this.ContainingTypeDeclaration.TypeDefinition, this.Name, this.Compilation.HostEnvironment.InternFactory);
      this.nestedTypeDefinition = result;
      result.AddTypeDeclaration(this);
      ITypeContract/*?*/ typeContract = this.Compilation.ContractProvider.GetTypeContractFor(this);
      if (typeContract != null)
        this.Compilation.ContractProvider.AssociateTypeWithContract(result, typeContract);
      return result;
    }

    public virtual bool IsNew {
      get {
        return (this.flags & Flags.New) != 0;
      }
    }

    protected abstract NestedTypeDeclaration MakeShallowCopy(List<ITypeDeclarationMember> members);
    //^ ensures result.GetType() == this.GetType();

    public abstract ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration);
    //^ ensures result.GetType() == this.GetType();
    //^ ensures result.ContainingTypeDeclaration == targetTypeDeclaration;

    /// <summary>
    /// The symbol table entity that corresponds to this source construct.
    /// </summary>
    public NestedTypeDefinition NestedTypeDefinition {
      get {
        if (this.nestedTypeDefinition == null) {
          lock (GlobalLock.LockingObject) {
            if (this.nestedTypeDefinition == null)
              this.nestedTypeDefinition = this.GetOrCreateNestedType();
          }
        }
        return this.nestedTypeDefinition; 
      }
    }
    NestedTypeDefinition/*?*/ nestedTypeDefinition;

    public override BlockStatement OuterDummyBlock {
      get {
        if (this.outerDummyBlock == null) {
          lock (GlobalLock.LockingObject) {
            if (this.outerDummyBlock == null) {
              this.outerDummyBlock = new BlockStatement(new List<Statement>(0), this.SourceLocation);
              this.outerDummyBlock.SetContainers(this.ContainingTypeDeclaration.DummyBlock, this);
            }
          }
        }
        return this.outerDummyBlock;
      }
    }
    BlockStatement/*?*/ outerDummyBlock;

    public virtual void SetContainingTypeDeclaration(TypeDeclaration containingTypeDeclaration, bool recurse) {
      this.containingTypeDeclaration = containingTypeDeclaration;
      this.OuterDummyBlock.SetContainers(containingTypeDeclaration.DummyBlock, this);
      this.SetCompilationPart(containingTypeDeclaration.CompilationPart, recurse);
    }

    public override TypeDefinition TypeDefinition {
      get { return this.NestedTypeDefinition; }
    }

    public override TypeDeclaration UpdateMembers(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit)
      //^^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDcoument);
      //^^ ensures result.GetType() == this.GetType();
    {
      NestedTypeDeclaration result = this.MakeShallowCopy(members);
      ISourceDocument afterEdit = edit.SourceDocumentAfterEdit;
      ISourceLocation locationBeforeEdit = this.SourceLocation;
      //^ assume afterEdit.IsUpdatedVersionOf(locationBeforeEdit.SourceDocument);
      result.sourceLocation = afterEdit.GetCorrespondingSourceLocation(locationBeforeEdit);
      List<ITypeDeclarationMember> newParentMembers = new List<ITypeDeclarationMember>(this.ContainingTypeDeclaration.TypeDeclarationMembers);
      TypeDeclaration containingTypeDeclaration = this.ContainingTypeDeclaration;
      //^ assume edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(containingTypeDeclaration.SourceLocation.SourceDocument);
      TypeDeclaration newParent = containingTypeDeclaration.UpdateMembers(newParentMembers, edit);
      result.containingTypeDeclaration = newParent;
      for (int i = 0, n = newParentMembers.Count; i < n; i++) {
        if (newParentMembers[i] == this) { newParentMembers[i] = result; break; }
      }
      return result;
    }

    public TypeMemberVisibility Visibility {
      get {
        return ((TypeMemberVisibility)this.flags) & TypeMemberVisibility.Mask;
      }
    }

    #region ITypeDeclarationMember Members

    TypeDeclaration ITypeDeclarationMember.ContainingTypeDeclaration {
      get { return this.ContainingTypeDeclaration; }
    }

    ITypeDeclarationMember ITypeDeclarationMember.MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration)
      //^^ requires targetTypeDeclaration.GetType() == this.ContainingTypeDeclaration.GetType();
      //^^ ensures result.GetType() == this.GetType();
      //^^ ensures result.ContainingTypeDeclaration == targetTypeDeclaration;
    {
      //^ assume targetTypeDeclaration is TypeDeclaration; //follows from the precondition
      return this.MakeShallowCopyFor((TypeDeclaration)targetTypeDeclaration);
    }

    ITypeDefinitionMember/*?*/ ITypeDeclarationMember.TypeDefinitionMember {
      get { return this.NestedTypeDefinition; }
    }

    #endregion

    #region IContainerMember<TypeDeclaration> Members

    TypeDeclaration IContainerMember<TypeDeclaration>.Container {
      get { return this.ContainingTypeDeclaration; }
    }

    IName IContainerMember<TypeDeclaration>.Name {
      get { return this.Name; }
    }

    #endregion

    #region IAggregatableMember Members

    ITypeDefinitionMember IAggregatableTypeDeclarationMember.AggregatedMember {
      get { return this.NestedTypeDefinition; }
    }

    #endregion

  }

  /// <summary>
  /// Corresponds to a source language type declaration, such as a C# partial class. One of more of these make up a type definition. 
  /// </summary>
  public abstract class TypeDeclaration : SourceItem, IContainer<IAggregatableTypeDeclarationMember>, IContainer<ITypeDeclarationMember>, IDeclaration, INamedEntity {
    [Flags]
    public enum Flags {
      None     = 0x00000000,

      Abstract = 0x40000000,
      New =      0x20000000,
      Partial =  0x10000000,
      Sealed =   0x08000000,
      Static =   0x04000000,
      Unsafe =   0x02000000,
    }

    protected readonly Flags flags;

    protected TypeDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, Flags flags, NameDeclaration name, 
      List<GenericTypeParameterDeclaration>/*?*/ genericParameters, List<TypeExpression> baseTypes, List<ITypeDeclarationMember> members, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.sourceAttributes = sourceAttributes;
      this.flags = flags;
      this.name = name;
      this.genericParameters = genericParameters;
      this.baseTypes = baseTypes;
      this.typeDeclarationMembers = members;
    }

    protected TypeDeclaration(CompilationPart compilationPart, TypeDeclaration template)
      : base(template.SourceLocation) {
      this.sourceAttributes = new List<SourceCustomAttribute>(template.SourceAttributes);
      this.flags = template.flags;
      this.compilationPart = compilationPart;
      this.name = template.Name.MakeCopyFor(compilationPart.Compilation);
      this.genericParameters = new List<GenericTypeParameterDeclaration>(template.GenericParameters);
      this.baseTypes = new List<TypeExpression>(template.BaseTypes);
      this.typeDeclarationMembers = new List<ITypeDeclarationMember>(template.typeDeclarationMembers);
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">A new value for containing block. This replaces template.ContainingBlock in the resulting copy of template.</param>
    /// <param name="template">The template to copy.</param>
    protected TypeDeclaration(BlockStatement containingBlock, TypeDeclaration template)
      : this(containingBlock, template, new List<ITypeDeclarationMember>(template.typeDeclarationMembers)) {
    }

    protected TypeDeclaration(BlockStatement containingBlock, TypeDeclaration template, List<ITypeDeclarationMember> members)
      : base(template.SourceLocation) {
      this.sourceAttributes = new List<SourceCustomAttribute>(template.SourceAttributes);
      this.flags = template.flags;
      this.compilationPart = containingBlock.CompilationPart;
      this.name = template.Name.MakeCopyFor(containingBlock.Compilation);
      this.genericParameters = new List<GenericTypeParameterDeclaration>(template.GenericParameters);
      this.baseTypes = new List<TypeExpression>(template.BaseTypes);
      this.typeDeclarationMembers = members;
    }

    /// <summary>
    /// Compute the offset of its field. For example, a struct in C and a union will have
    /// different say about its field's offset. 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public virtual uint GetFieldOffset(object item)
    {
      return 0;
    }

    public void AddHelperMember(ITypeDeclarationMember typeDeclarationMember) {
      lock (this) {
        if (this.helperMembers == null)
          this.helperMembers = new List<ITypeDeclarationMember>();
        this.helperMembers.Add(typeDeclarationMember);
      }
    }

    public virtual ushort Alignment {
      get { return 0; } //TODO: provide a default implementation that extracts this from a custom attribute
    }

    public virtual IEnumerable<ICustomAttribute> Attributes {
      get {
        foreach (SourceCustomAttribute attribute in this.SourceAttributes) {
          yield return new CustomAttribute(attribute);
          //TODO: filter out pseudo custom attributes
        }
        //TODO: cache the result of this property
      } 
    }

    /// <summary>
    /// A collection of expressions that refer to the base types (classes and interfaces) of this type.
    /// </summary>
    public IEnumerable<TypeExpression> BaseTypes {
      get {
        for (int i = 0, n = this.baseTypes.Count; i < n; i++)
          yield return this.baseTypes[i] = (TypeExpression)this.baseTypes[i].MakeCopyFor(this.OuterDummyBlock);
      }
    }
    readonly List<TypeExpression> baseTypes;

    /// <summary>
    /// A map from names to resolved metadata items. Use this table for case insensitive lookup.
    /// Do not use this dictionary unless you are implementing SimpleName.ResolveUsing(TypeDeclaration typeDeclaration). 
    /// </summary>
    internal Dictionary<int, object/*?*/> caseInsensitiveCache = new Dictionary<int, object/*?*/>();
    /// <summary>
    /// A map from names to resolved metadata items. Use this table for case sensitive lookup.
    /// Do not use this dictionary unless you are implementing SimpleName.ResolveUsing(TypeDeclaration typeDeclaration). 
    /// </summary>
    internal Dictionary<int, object/*?*/> caseSensitiveCache = new Dictionary<int, object/*?*/>();

    public Compilation Compilation {
      get { return this.CompilationPart.Compilation; }
    }

    public CompilationPart CompilationPart {
      get { 
        //^ assume this.compilationPart != null;
        return this.compilationPart;
      }
    }
    //^ [SpecPublic]
    CompilationPart/*?*/ compilationPart;

    public abstract BlockStatement DummyBlock { get; }

    /// <summary>
    /// The type parameters, if any, of this type.
    /// </summary>
    public IEnumerable<GenericTypeParameterDeclaration> GenericParameters {
      get {
        List<GenericTypeParameterDeclaration> genericParameters;
        if (this.genericParameters == null)
          yield break;
        else
          genericParameters = this.genericParameters;
        for (int i = 0, n = genericParameters.Count; i < n; i++)
          yield return genericParameters[i] = genericParameters[i].MakeCopyFor(this);
      }
    }
    readonly List<GenericTypeParameterDeclaration>/*?*/ genericParameters;

    public virtual ushort GenericParameterCount {
      get {
        return (ushort)(this.genericParameters == null ? 0 : this.genericParameters.Count);
      }
    }

    /// <summary>
    /// The visibility of type members that do not explicitly specify their visibility
    /// (their ITypeMember.Visibility values is TypeMemberVisibility.Default).
    /// </summary>
    public virtual TypeMemberVisibility GetDefaultVisibility() {
      return TypeMemberVisibility.Private;
    }

    /// <summary>
    /// An instance of a language specific class containing methods that are of general utility. 
    /// </summary>
    public LanguageSpecificCompilationHelper Helper {
      get { return this.CompilationPart.Helper; }
    }

    /// <summary>
    /// If true, instances of this type will all be instances of some subtype of this type.
    /// </summary>
    public virtual bool IsAbstract {
      get {
        return (this.flags & Flags.Abstract) != 0;
      }
    }

    /// <summary>
    /// If true, this type declaration may be aggregated with other type declarations into a single type definition.
    /// </summary>
    public virtual bool IsPartial {
      get {
        return (this.flags & Flags.Partial) != 0;
      }
    }

    /// <summary>
    /// If true, this type has no subtypes.
    /// </summary>
    public virtual bool IsSealed {
      get {
        return (this.flags & Flags.Sealed) != 0;
      }
    }

    public virtual bool IsStatic {
      get {
        return (this.flags & Flags.Static) != 0;
      }
    }

    /// <summary>
    /// If true, this type can contain "unsafe" constructs such as pointers.
    /// </summary>
    public virtual bool IsUnsafe {
      get {
        return (this.flags & Flags.Unsafe) != 0;
      }
    }

    /// <summary>
    /// Layout of the type declaration.
    /// </summary>
    public virtual LayoutKind Layout {
      get {
        return LayoutKind.Auto; //TODO: get this from a custom attribute
      }
    }

    /// <summary>
    /// The name of the type.
    /// </summary>
    public NameDeclaration Name {
      get {
        return this.name;
      }
    }
    readonly NameDeclaration name;

    public abstract BlockStatement OuterDummyBlock { get; }

    /// <summary>
    /// A possibly empty collection of type members that are added by the compiler to help with the implementation of language features.
    /// </summary>
    public virtual IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get {
        if (this.helperMembers == null)
          yield break;
        else {
          List<ITypeDeclarationMember> helperMembers = this.helperMembers;
          // iterate with index and not foreach because the list may change while we iterate over it
          for (int i = 0; i < helperMembers.Count; i++) {
            ITypeDefinitionMember/*?*/ hmemDef = helperMembers[i].TypeDefinitionMember;
            if (hmemDef != null) yield return hmemDef;
          }
        }
      }
    }
    List<ITypeDeclarationMember>/*?*/ helperMembers;

    /// <summary>
    /// A collection of metadata declarative security attributes that are associated with this type.
    /// </summary>
    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); } //TODO: extract these from the source attributes
    }

    public virtual void SetCompilationPart(CompilationPart compilationPart, bool recurse) {
      this.compilationPart = compilationPart;
      if (!recurse) return;
      BlockStatement dummyBlock = this.OuterDummyBlock;
      DummyExpression containingExpression = new DummyExpression(dummyBlock, SourceDummy.SourceLocation);
      if (this.sourceAttributes != null)
        foreach (SourceCustomAttribute attribute in this.sourceAttributes) attribute.SetContainingExpression(containingExpression);
      if (this.genericParameters != null)
        foreach (GenericTypeParameterDeclaration genericParameter in this.genericParameters) genericParameter.SetContainingExpression(containingExpression);
      foreach (TypeExpression baseType in this.baseTypes) baseType.SetContainingExpression(containingExpression);
      foreach (ITypeDeclarationMember member in this.typeDeclarationMembers) this.SetMemberContainingTypeDeclaration(member);
      TypeContract/*?*/ typeContract = this.Compilation.ContractProvider.GetTypeContractFor(this) as TypeContract;
      if (typeContract != null) typeContract.SetContainingType(this);
    }

    public virtual void SetMemberContainingTypeDeclaration(ITypeDeclarationMember member) {
      TypeDeclarationMember/*?*/ tmem = member as TypeDeclarationMember;
      if (tmem != null) {
        tmem.SetContainingTypeDeclaration(this, true); 
        return;
      }
      NestedTypeDeclaration/*?*/ ntdecl = member as NestedTypeDeclaration;
      if (ntdecl != null) {
        ntdecl.SetContainingTypeDeclaration(this, true); 
        return;
      }
    }

    /// <summary>
    /// Size of an object of this type. In bytes. If zero, the size is unspecified and will be determined at runtime.
    /// </summary>
    public virtual uint SizeOf {
      get {
        //TODO: run through the attributes and see if one of them specifies the size of the type.
        return 0;
      }
    }

    public IEnumerable<SourceCustomAttribute> SourceAttributes {
      get {
        List<SourceCustomAttribute> sourceAttributes;
        if (this.sourceAttributes == null)
          yield break;
        else
          sourceAttributes = this.sourceAttributes;
        for (int i = 0, n = sourceAttributes.Count; i < n; i++)
          yield return sourceAttributes[i] = sourceAttributes[i].MakeShallowCopyFor(this.OuterDummyBlock);
      }
    }
    readonly List<SourceCustomAttribute>/*?*/ sourceAttributes;

    //^ [Confined]
    public override string ToString() {
      return this.Helper.GetTypeName(this.TypeDefinition);
    }

    /// <summary>
    /// The collection of things that are considered members of this type. For example: events, fields, method, properties and nested types.
    /// </summary>
    public IEnumerable<ITypeDeclarationMember> TypeDeclarationMembers {
      get {
        for (int i = 0, n = this.typeDeclarationMembers.Count; i < n; i++)
          yield return this.typeDeclarationMembers[i] = this.typeDeclarationMembers[i].MakeShallowCopyFor(this);
      }
    }
    readonly List<ITypeDeclarationMember> typeDeclarationMembers;

    public IEnumerable<ITypeDeclarationMember> GetTypeDeclarationMembersNamed(int uniqueKey) {
      this.InitializeIfNecessary();
      List<ITypeDeclarationMember> members;
      if (this.caseSensitiveMemberNameToMemberListMap.TryGetValue(uniqueKey, out members)) {
        List<ITypeDeclarationMember> result = new List<ITypeDeclarationMember>(members.Count);
        foreach (var member in members) 
          result.Add(member.MakeShallowCopyFor(this));
        return result;
      } else {
        return emptyMemberList;
      }
    }

    private static readonly  List<ITypeDeclarationMember> emptyMemberList = new List<ITypeDeclarationMember>(0);

    private void InitializeIfNecessary() {
      if (this.caseSensitiveMemberNameToMemberListMap == null) {
        this.caseSensitiveMemberNameToMemberListMap = new Dictionary<int, List<ITypeDeclarationMember>>();
        foreach (var member in this.typeDeclarationMembers) {
          List<ITypeDeclarationMember> membersNamed;
          if (!this.caseSensitiveMemberNameToMemberListMap.TryGetValue(member.Name.UniqueKey, out membersNamed)) {
            membersNamed = new List<ITypeDeclarationMember>();
            this.caseSensitiveMemberNameToMemberListMap[member.Name.UniqueKey] = membersNamed;
          }
          membersNamed.Add(member);
        }
      }
    }

    private Dictionary<int, List<ITypeDeclarationMember>> caseSensitiveMemberNameToMemberListMap = null;

    /// <summary>
    /// The symbol table type definition that corresponds to this type declaration. If this type declaration is a partial type, the symbol table type
    /// will be an aggregate of multiple type declarations.
    /// </summary>
    public abstract TypeDefinition TypeDefinition {
      get;
    }

    public abstract TypeDeclaration UpdateMembers(List<ITypeDeclarationMember> members, ISourceDocumentEdit edit);
    //^ requires edit.SourceDocumentAfterEdit.IsUpdatedVersionOf(this.SourceLocation.SourceDocument);
    //^ ensures result.GetType() == this.GetType();

    #region IContainer<IAggregatableTypeDeclarationMember> Members

    IEnumerable<IAggregatableTypeDeclarationMember> IContainer<IAggregatableTypeDeclarationMember>.Members {
      get {
        return IteratorHelper.GetFilterEnumerable<ITypeDeclarationMember, IAggregatableTypeDeclarationMember>(this.TypeDeclarationMembers);
      }
    }

    #endregion

    #region IContainer<ITypeDeclarationMember> Members

    IEnumerable<ITypeDeclarationMember> IContainer<ITypeDeclarationMember>.Members {
      get {
        return this.TypeDeclarationMembers;
      }
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion


  }

}
