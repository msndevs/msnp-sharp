﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.5448
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by Microsoft.VSDesigner, Version 2.0.50727.5448.
// 
#pragma warning disable 1591

namespace MSNPSharp.MSNWS.MSNDirectoryService {
    using System.Diagnostics;
    using System.Web.Services;
    using System.ComponentModel;
    using System.Web.Services.Protocols;
    using System;
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.5420")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="DirectoryServiceBinding", Namespace="http://profile.live.com/")]
    public partial class DirectoryService : System.Web.Services.Protocols.SoapHttpClientProtocol {
        
        private SOAPApplicationHeader sOAPApplicationHeaderValueField;
        
        private SOAPUserHeader sOAPUserHeaderValueField;
        
        private System.Threading.SendOrPostCallback GetOperationCompleted;
        
        private System.Threading.SendOrPostCallback GetManyOperationCompleted;
        
        private System.Threading.SendOrPostCallback SetOperationCompleted;
        
        private bool useDefaultCredentialsSetExplicitly;
        
        /// <remarks/>
        public DirectoryService() {
            this.Url = "https://directory.services.live.com/profile/profile.asmx";
            if ((this.IsLocalFileSystemWebService(this.Url) == true)) {
                this.UseDefaultCredentials = true;
                this.useDefaultCredentialsSetExplicitly = false;
            }
            else {
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        public SOAPApplicationHeader SOAPApplicationHeaderValue {
            get {
                return this.sOAPApplicationHeaderValueField;
            }
            set {
                this.sOAPApplicationHeaderValueField = value;
            }
        }
        
        public SOAPUserHeader SOAPUserHeaderValue {
            get {
                return this.sOAPUserHeaderValueField;
            }
            set {
                this.sOAPUserHeaderValueField = value;
            }
        }
        
        public new string Url {
            get {
                return base.Url;
            }
            set {
                if ((((this.IsLocalFileSystemWebService(base.Url) == true) 
                            && (this.useDefaultCredentialsSetExplicitly == false)) 
                            && (this.IsLocalFileSystemWebService(value) == false))) {
                    base.UseDefaultCredentials = false;
                }
                base.Url = value;
            }
        }
        
        public new bool UseDefaultCredentials {
            get {
                return base.UseDefaultCredentials;
            }
            set {
                base.UseDefaultCredentials = value;
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        /// <remarks/>
        public event GetCompletedEventHandler GetCompleted;
        
        /// <remarks/>
        public event GetManyCompletedEventHandler GetManyCompleted;
        
        /// <remarks/>
        public event SetCompletedEventHandler SetCompleted;
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("SOAPUserHeaderValue")]
        [System.Web.Services.Protocols.SoapHeaderAttribute("SOAPApplicationHeaderValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://profile.live.com/Get", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        [return: System.Xml.Serialization.XmlElementAttribute("GetResponse", Namespace="http://profile.live.com/")]
        public GetResponse Get([System.Xml.Serialization.XmlElementAttribute("Get", Namespace="http://profile.live.com/")] GetRequestType Get1) {
            object[] results = this.Invoke("Get", new object[] {
                        Get1});
            return ((GetResponse)(results[0]));
        }
        
        /// <remarks/>
        public void GetAsync(GetRequestType Get1) {
            this.GetAsync(Get1, null);
        }
        
        /// <remarks/>
        public void GetAsync(GetRequestType Get1, object userState) {
            if ((this.GetOperationCompleted == null)) {
                this.GetOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetOperationCompleted);
            }
            this.InvokeAsync("Get", new object[] {
                        Get1}, this.GetOperationCompleted, userState);
        }
        
        private void OnGetOperationCompleted(object arg) {
            if ((this.GetCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetCompleted(this, new GetCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("SOAPUserHeaderValue")]
        [System.Web.Services.Protocols.SoapHeaderAttribute("SOAPApplicationHeaderValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://profile.live.com/GetMany", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        [return: System.Xml.Serialization.XmlElementAttribute("GetManyResponse", Namespace="http://profile.live.com/")]
        public GetManyResponse GetMany([System.Xml.Serialization.XmlElementAttribute("GetMany", Namespace="http://profile.live.com/")] GetManyRequestType GetMany1) {
            object[] results = this.Invoke("GetMany", new object[] {
                        GetMany1});
            return ((GetManyResponse)(results[0]));
        }
        
        /// <remarks/>
        public void GetManyAsync(GetManyRequestType GetMany1) {
            this.GetManyAsync(GetMany1, null);
        }
        
        /// <remarks/>
        public void GetManyAsync(GetManyRequestType GetMany1, object userState) {
            if ((this.GetManyOperationCompleted == null)) {
                this.GetManyOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetManyOperationCompleted);
            }
            this.InvokeAsync("GetMany", new object[] {
                        GetMany1}, this.GetManyOperationCompleted, userState);
        }
        
        private void OnGetManyOperationCompleted(object arg) {
            if ((this.GetManyCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetManyCompleted(this, new GetManyCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapHeaderAttribute("SOAPUserHeaderValue")]
        [System.Web.Services.Protocols.SoapHeaderAttribute("SOAPApplicationHeaderValue")]
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://profile.live.com/Set", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Bare)]
        [return: System.Xml.Serialization.XmlElementAttribute("SetResponse", Namespace="http://profile.live.com/")]
        public SetResultType Set([System.Xml.Serialization.XmlElementAttribute("Set", Namespace="http://profile.live.com/")] SetRequestType Set1) {
            object[] results = this.Invoke("Set", new object[] {
                        Set1});
            return ((SetResultType)(results[0]));
        }
        
        /// <remarks/>
        public void SetAsync(SetRequestType Set1) {
            this.SetAsync(Set1, null);
        }
        
        /// <remarks/>
        public void SetAsync(SetRequestType Set1, object userState) {
            if ((this.SetOperationCompleted == null)) {
                this.SetOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSetOperationCompleted);
            }
            this.InvokeAsync("Set", new object[] {
                        Set1}, this.SetOperationCompleted, userState);
        }
        
        private void OnSetOperationCompleted(object arg) {
            if ((this.SetCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SetCompleted(this, new SetCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        public new void CancelAsync(object userState) {
            base.CancelAsync(userState);
        }
        
        private bool IsLocalFileSystemWebService(string url) {
            if (((url == null) 
                        || (url == string.Empty))) {
                return false;
            }
            System.Uri wsUri = new System.Uri(url);
            if (((wsUri.Port >= 1024) 
                        && (string.Compare(wsUri.Host, "localHost", System.StringComparison.OrdinalIgnoreCase) == 0))) {
                return true;
            }
            return false;
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://profile.live.com/")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://profile.live.com/", IsNullable=false)]
    public partial class SOAPUserHeader : System.Web.Services.Protocols.SoapHeader {
        
        private string ticketTokenField;
        
        /// <remarks/>
        public string TicketToken {
            get {
                return this.ticketTokenField;
            }
            set {
                this.ticketTokenField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://profile.live.com/")]
    public partial class SetResultType {
        
        private CallStatisticsType callStatisticsField;
        
        private IdType idField;
        
        private ViewType viewField;
        
        /// <remarks/>
        public CallStatisticsType CallStatistics {
            get {
                return this.callStatisticsField;
            }
            set {
                this.callStatisticsField = value;
            }
        }
        
        /// <remarks/>
        public IdType Id {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
            }
        }
        
        /// <remarks/>
        public ViewType View {
            get {
                return this.viewField;
            }
            set {
                this.viewField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://profile.live.com/")]
    public partial class CallStatisticsType {
        
        private string serverNameField;
        
        private string elapsedMillisecondsField;
        
        /// <remarks/>
        public string ServerName {
            get {
                return this.serverNameField;
            }
            set {
                this.serverNameField = value;
            }
        }
        
        /// <remarks/>
        public string ElapsedMilliseconds {
            get {
                return this.elapsedMillisecondsField;
            }
            set {
                this.elapsedMillisecondsField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://profile.live.com/")]
    public partial class IdType {
        
        private string ns1Field;
        
        private object v1Field;
        
        private string ns2Field;
        
        private object v2Field;
        
        private string ns3Field;
        
        private object v3Field;
        
        /// <remarks/>
        public string Ns1 {
            get {
                return this.ns1Field;
            }
            set {
                this.ns1Field = value;
            }
        }
        
        /// <remarks/>
        public object V1 {
            get {
                return this.v1Field;
            }
            set {
                this.v1Field = value;
            }
        }
        
        /// <remarks/>
        public string Ns2 {
            get {
                return this.ns2Field;
            }
            set {
                this.ns2Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable=true)]
        public object V2 {
            get {
                return this.v2Field;
            }
            set {
                this.v2Field = value;
            }
        }
        
        /// <remarks/>
        public string Ns3 {
            get {
                return this.ns3Field;
            }
            set {
                this.ns3Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(IsNullable=true)]
        public object V3 {
            get {
                return this.v3Field;
            }
            set {
                this.v3Field = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://profile.live.com/")]
    public partial class ViewType {
        
        private string viewNameField;
        
        private string viewVersionField;
        
        private AttributeType[] attributesField;
        
        /// <remarks/>
        public string ViewName {
            get {
                return this.viewNameField;
            }
            set {
                this.viewNameField = value;
            }
        }
        
        /// <remarks/>
        public string ViewVersion {
            get {
                return this.viewVersionField;
            }
            set {
                this.viewVersionField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("A")]
        public AttributeType[] Attributes {
            get {
                return this.attributesField;
            }
            set {
                this.attributesField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://profile.live.com/")]
    public partial class AttributeType {
        
        private string nField;
        
        private object vField;
        
        /// <remarks/>
        public string N {
            get {
                return this.nField;
            }
            set {
                this.nField = value;
            }
        }
        
        /// <remarks/>
        public object V {
            get {
                return this.vField;
            }
            set {
                this.vField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://profile.live.com/")]
    public partial class SetRequestType {
        
        private SetRequestTypeRequest requestField;
        
        /// <remarks/>
        public SetRequestTypeRequest request {
            get {
                return this.requestField;
            }
            set {
                this.requestField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://profile.live.com/")]
    public partial class SetRequestTypeRequest {
        
        private string viewNameField;
        
        private IdType idField;
        
        private bool getUpdatedProfileOnSetField;
        
        private bool getUpdatedProfileOnSetFieldSpecified;
        
        /// <remarks/>
        public string ViewName {
            get {
                return this.viewNameField;
            }
            set {
                this.viewNameField = value;
            }
        }
        
        /// <remarks/>
        public IdType Id {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
            }
        }
        
        /// <remarks/>
        public bool GetUpdatedProfileOnSet {
            get {
                return this.getUpdatedProfileOnSetField;
            }
            set {
                this.getUpdatedProfileOnSetField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool GetUpdatedProfileOnSetSpecified {
            get {
                return this.getUpdatedProfileOnSetFieldSpecified;
            }
            set {
                this.getUpdatedProfileOnSetFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://profile.live.com/")]
    public partial class GetManyResultType {
        
        private CallStatisticsType callStatisticsField;
        
        private IdType[] idsField;
        
        private ViewType[] viewsField;
        
        /// <remarks/>
        public CallStatisticsType CallStatistics {
            get {
                return this.callStatisticsField;
            }
            set {
                this.callStatisticsField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("ProfileId")]
        public IdType[] Ids {
            get {
                return this.idsField;
            }
            set {
                this.idsField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("View")]
        public ViewType[] Views {
            get {
                return this.viewsField;
            }
            set {
                this.viewsField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://profile.live.com/")]
    public partial class GetManyRequestType {
        
        private GetManyRequestTypeRequest requestField;
        
        /// <remarks/>
        public GetManyRequestTypeRequest request {
            get {
                return this.requestField;
            }
            set {
                this.requestField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://profile.live.com/")]
    public partial class GetManyRequestTypeRequest {
        
        private string viewNameField;
        
        private IdType[] idsField;
        
        /// <remarks/>
        public string ViewName {
            get {
                return this.viewNameField;
            }
            set {
                this.viewNameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("ProfileId")]
        public IdType[] Ids {
            get {
                return this.idsField;
            }
            set {
                this.idsField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://profile.live.com/")]
    public partial class GetResultType {
        
        private CallStatisticsType callStatisticsField;
        
        private IdType idField;
        
        private ViewType viewField;
        
        /// <remarks/>
        public CallStatisticsType CallStatistics {
            get {
                return this.callStatisticsField;
            }
            set {
                this.callStatisticsField = value;
            }
        }
        
        /// <remarks/>
        public IdType Id {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
            }
        }
        
        /// <remarks/>
        public ViewType View {
            get {
                return this.viewField;
            }
            set {
                this.viewField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://profile.live.com/")]
    public partial class GetRequestType {
        
        private GetRequestTypeRequest requestField;
        
        /// <remarks/>
        public GetRequestTypeRequest request {
            get {
                return this.requestField;
            }
            set {
                this.requestField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://profile.live.com/")]
    public partial class GetRequestTypeRequest {
        
        private string viewNameField;
        
        private IdType idField;
        
        /// <remarks/>
        public string ViewName {
            get {
                return this.viewNameField;
            }
            set {
                this.viewNameField = value;
            }
        }
        
        /// <remarks/>
        public IdType Id {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://profile.live.com/")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://profile.live.com/", IsNullable=false)]
    public partial class SOAPApplicationHeader : System.Web.Services.Protocols.SoapHeader {
        
        private string applicationIdField;
        
        private string scenarioField;
        
        /// <remarks/>
        public string ApplicationId {
            get {
                return this.applicationIdField;
            }
            set {
                this.applicationIdField = value;
            }
        }
        
        /// <remarks/>
        public string Scenario {
            get {
                return this.scenarioField;
            }
            set {
                this.scenarioField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://profile.live.com/")]
    public partial class GetResponse {
        
        private GetResultType getResultField;
        
        /// <remarks/>
        public GetResultType GetResult {
            get {
                return this.getResultField;
            }
            set {
                this.getResultField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.5420")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://profile.live.com/")]
    public partial class GetManyResponse {
        
        private GetManyResultType getManyResultField;
        
        /// <remarks/>
        public GetManyResultType GetManyResult {
            get {
                return this.getManyResultField;
            }
            set {
                this.getManyResultField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.5420")]
    public delegate void GetCompletedEventHandler(object sender, GetCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.5420")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public GetResponse Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((GetResponse)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.5420")]
    public delegate void GetManyCompletedEventHandler(object sender, GetManyCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.5420")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetManyCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetManyCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public GetManyResponse Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((GetManyResponse)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.5420")]
    public delegate void SetCompletedEventHandler(object sender, SetCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "2.0.50727.5420")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class SetCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal SetCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public SetResultType Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((SetResultType)(this.results[0]));
            }
        }
    }
}

#pragma warning restore 1591