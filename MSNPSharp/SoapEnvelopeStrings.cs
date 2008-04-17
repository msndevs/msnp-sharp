using System;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp
{
    static class SoapEnvelopeStrings
    {
        /// <summary>
        /// Request membership list
        /// </summary>
        public static string MemberShipRequestEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version='1.0' encoding='utf-8'?>"
                            + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">"
                            + "<soap:Header xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">"
                            + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                            + "<ApplicationId xmlns=\"http://www.msn.com/webservices/AddressBook\">09607671-1C32-421F-A6A6-CBFAA51AB5F4</ApplicationId>"
                            + "<IsMigration xmlns=\"http://www.msn.com/webservices/AddressBook\">false</IsMigration>"
                            + "<PartnerScenario xmlns=\"http://www.msn.com/webservices/AddressBook\">Initial</PartnerScenario>"
                            + "</ABApplicationHeader>"
                            + "<ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                            + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                            + "<TicketToken>{ticket}</TicketToken>"
                            + "</ABAuthHeader>"
                            + "</soap:Header>"
                            + "<soap:Body xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">"
                            + "<FindMembership xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                            + "<serviceFilter xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                            + "<Types xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                            + "<ServiceType xmlns=\"http://www.msn.com/webservices/AddressBook\">Messenger</ServiceType>"
                            + "<ServiceType xmlns=\"http://www.msn.com/webservices/AddressBook\">Invitation</ServiceType>"
                            + "<ServiceType xmlns=\"http://www.msn.com/webservices/AddressBook\">SocialNetwork</ServiceType>"
                            + "<ServiceType xmlns=\"http://www.msn.com/webservices/AddressBook\">Space</ServiceType>"
                            + "<ServiceType xmlns=\"http://www.msn.com/webservices/AddressBook\">Profile</ServiceType>"
                            + "</Types>"
                            + "</serviceFilter>"
                            + "<View xmlns=\"http://www.msn.com/webservices/AddressBook\">Full</View>"
                            + "<deltasOnly xmlns=\"http://www.msn.com/webservices/AddressBook\">{deltasOnly}</deltasOnly>"
                            + "<lastChange xmlns=\"http://www.msn.com/webservices/AddressBook\">{last_change}</lastChange>"
                            + "</FindMembership>"
                            + "</soap:Body>"
                            + "</soap:Envelope>";
                #endregion
            }
        }

        /// <summary>
        /// Request addressbook
        /// </summary>
        public static string AddressBookRequestEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                    + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                    + "<soap:Header>"
                    + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<ApplicationId>09607671-1C32-421F-A6A6-CBFAA51AB5F4</ApplicationId>"
                    //                                      xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
                    + "<IsMigration>false</IsMigration>"
                    + "<PartnerScenario>Initial</PartnerScenario>"
                    + "</ABApplicationHeader>"
                    + "<ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                    + "<TicketToken>{ticket}</TicketToken>"
                    + "</ABAuthHeader>"
                    + "</soap:Header>"
                    + "<soap:Body>"
                    + "<ABFindAll xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<abId>00000000-0000-0000-0000-000000000000</abId>"
                    //                             xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
                    + "<abView>Full</abView>"
                    + "<deltasOnly>{deltasOnly}</deltasOnly>"
                    + "<lastChange>{last_change}</lastChange>"
                    + "<dynamicItemView>Gleam</dynamicItemView>"
                    + "<dynamicItemLastChange>{dynamicItemLastChange}</dynamicItemLastChange>"
                    + "</ABFindAll>"
                    + "</soap:Body>"
                    + "</soap:Envelope>";
                #endregion
            }
        }

        public static string RemoveContactEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
 + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                                + "<soap:Header>"
                                + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ApplicationId>996CDE1E-AA53-4477-B943-2BE802EA6166</ApplicationId>"
                                + "<IsMigration>false</IsMigration>"
                                + "<PartnerScenario>Timer</PartnerScenario>"
                                + "<CacheKey>{cachekey}</CacheKey>"
                                + "</ABApplicationHeader>"
                                + "<ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                                + "<TicketToken>{ticket}</TicketToken>"
                                + "</ABAuthHeader></soap:Header><soap:Body>"
                                + "<ABContactDelete xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<abId>00000000-0000-0000-0000-000000000000</abId>"
                                + "<contacts><Contact>"
                                + "<contactId>{guid}</contactId>"
                                + "</Contact></contacts>"
                                + "</ABContactDelete>"
                                + "</soap:Body>"
                                + "</soap:Envelope>";
                #endregion
            }
        }

        public static string RemoveContactFromGroupEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
 + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                                + "<soap:Header>"
                                + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ApplicationId>996CDE1E-AA53-4477-B943-2BE802EA6166</ApplicationId>"
                                + "<IsMigration>false</IsMigration>"
                                + "<PartnerScenario>GroupSave</PartnerScenario>"
                                + "<CacheKey>{cachekey}</CacheKey>"
                                + "</ABApplicationHeader>"
                                + "<ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                                + "<TicketToken>{ticket}</TicketToken>"
                                + "</ABAuthHeader></soap:Header><soap:Body>"
                                + "<ABGroupContactDelete xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<abId>00000000-0000-0000-0000-000000000000</abId>"
                                + "<contacts><Contact>"
                                + "<contactId>{contact_guid}</contactId>"
                                + "</Contact></contacts>"
                                + "<groupFilter><groupIds>"
                                + "<guid>{group_guid}</guid>"
                                + "</groupIds></groupFilter></ABGroupContactDelete></soap:Body>"
                                + "</soap:Envelope>";
                #endregion
                ;
            }
        }

        public static string RemoveGroupEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                                + "<soap:Header>"
                                + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ApplicationId>996CDE1E-AA53-4477-B943-2BE802EA6166</ApplicationId>"
                                + "<IsMigration>false</IsMigration>"
                                + "<PartnerScenario>Timer</PartnerScenario>"
                                + "<CacheKey>{cachekey}</CacheKey>"
                                + "</ABApplicationHeader>"
                                + "<ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                                + "<TicketToken>{ticket}</TicketToken>"
                                + "</ABAuthHeader>"
                                + "</soap:Header>"
                                + "<soap:Body>"
                                + "<ABGroupDelete xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<abId>00000000-0000-0000-0000-000000000000</abId>"
                                + "<groupFilter><groupIds>"
                                + "<guid>{guid}</guid>"
                                + "</groupIds></groupFilter></ABGroupDelete></soap:Body>"
                                + "</soap:Envelope>";
                #endregion
            }
        }

        public static string RenameGroupEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                                + "<soap:Header>"
                                + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ApplicationId>996CDE1E-AA53-4477-B943-2BE802EA6166</ApplicationId>"
                                + "<IsMigration>false</IsMigration>"
                                + "<PartnerScenario>GroupSave</PartnerScenario>"
                                + "<CacheKey>{cachekey}</CacheKey>"
                                + "</ABApplicationHeader><ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                                + "<TicketToken>{ticket}</TicketToken>"
                                + "</ABAuthHeader></soap:Header><soap:Body>"
                                + "<ABGroupUpdate xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<abId>00000000-0000-0000-0000-000000000000</abId>"
                                + "<groups><Group>"
                                + "<groupId>{guid}</groupId>"
                                + "<groupInfo>"
                                + "<name>{newname}</name>"
                                + "</groupInfo>"
                                + "<propertiesChanged>GroupName</propertiesChanged>"
                                + "</Group></groups></ABGroupUpdate></soap:Body>"
                                + "</soap:Envelope>";
                #endregion
            }
        }

        public static string AddContactToGroupEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                            + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                                + "<soap:Header>"
                                + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ApplicationId>996CDE1E-AA53-4477-B943-2BE802EA6166</ApplicationId>"
                                + "<IsMigration>false</IsMigration>"
                                + "<PartnerScenario>GroupSave</PartnerScenario>"
                                + "<CacheKey>{cachekey}</CacheKey>"
                                + "</ABApplicationHeader><ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                                + "<TicketToken>{ticket}</TicketToken>"
                                + "</ABAuthHeader></soap:Header><soap:Body>"
                                + "<ABGroupContactAdd xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<abId>00000000-0000-0000-0000-000000000000</abId>"
                                + "<groupFilter><groupIds>"
                                + "<guid>{group_guid}</guid>"
                                + "</groupIds></groupFilter><contacts><Contact>"
                                + "<contactId>{contact_guid}</contactId>"
                                + "</Contact></contacts></ABGroupContactAdd></soap:Body>"
                                + "</soap:Envelope>";
                #endregion
            }
        }

        public static string AddGroupEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                                + "<soap:Header>"
                                + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ApplicationId>996CDE1E-AA53-4477-B943-2BE802EA6166</ApplicationId>"
                                + "<IsMigration>false</IsMigration>"
                                + "<PartnerScenario>GroupSave</PartnerScenario>"
                                + "<CacheKey>{cachkey}</CacheKey>"
                                + "</ABApplicationHeader>"
                                + "<ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                                + "<TicketToken>{ticket}</TicketToken>"
                                + "</ABAuthHeader>"
                                + "</soap:Header>"
                                + "<soap:Body><ABGroupAdd xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<abId>00000000-0000-0000-0000-000000000000</abId>"
                                + "<groupAddOptions>"
                                + "<fRenameOnMsgrConflict>false</fRenameOnMsgrConflict>"
                                + "</groupAddOptions><groupInfo><GroupInfo>"
                                + "<name>{groupname}</name>"
                                + "<groupType>C8529CE2-6EAD-434d-881F-341E17DB3FF8</groupType>"
                                + "<fMessenger>false</fMessenger>"
                                + "<annotations><Annotation>"
                                + "<Name>MSN.IM.Display</Name>"
                                + "<Value>1</Value>"
                                + "</Annotation></annotations></GroupInfo></groupInfo>"
                                + "</ABGroupAdd></soap:Body>"
                                + "</soap:Envelope>";
                #endregion
            }
        }

        public static string AddContactEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                                + "<soap:Header>"
                                + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ApplicationId>996CDE1E-AA53-4477-B943-2BE802EA6166</ApplicationId>"
                                + "<IsMigration>false</IsMigration>"
                                + "<PartnerScenario>ContactSave</PartnerScenario>"
                                + "<CacheKey>{cachekey}</CacheKey>"
                                + "</ABApplicationHeader>"
                                + "<ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                                + "<TicketToken>{ticket}</TicketToken>"
                                + "</ABAuthHeader></soap:Header><soap:Body>"
                                + "<ABContactAdd xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<abId>00000000-0000-0000-0000-000000000000</abId>"
                                + "<contacts><Contact xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                                + "<contactInfo>"
                                + "<contactType>LivePending</contactType>"
                                + "<passportName>{account}</passportName>"
                                + "<isMessengerUser>true</isMessengerUser>"
                                + "<MessengerMemberInfo>"
                                + "<DisplayName>{displayname}</DisplayName>"
                                + "</MessengerMemberInfo>"
                                + "</contactInfo></Contact></contacts>"
                                + "<options>"
                                + "<EnableAllowListManagement>true</EnableAllowListManagement>"
                                + "</options></ABContactAdd></soap:Body>"
                                + "</soap:Envelope>";
                #endregion
            }
        }

        /// <summary>
        /// Get the OIM message
        /// </summary>
        public static string OIMRequestEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                          + "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">"
                                          + "<soap:Header>"
                                          + "<PassportCookie xmlns=\"http://www.hotmail.msn.com/ws/2004/09/oim/rsi\">"
                                          + "<t>{t}</t>"
                                          + "<p>{p}</p>"
                                          + "</PassportCookie>"
                                          + "</soap:Header><soap:Body>"
                                          + "<GetMessage xmlns=\"http://www.hotmail.msn.com/ws/2004/09/oim/rsi\">"
                                          + "<messageId>{MessageID}</messageId>"
                                          + "<alsoMarkAsRead>false</alsoMarkAsRead>"
                                          + "</GetMessage></soap:Body></soap:Envelope>";
                #endregion
            }
        }

        public static string OIMDeleteEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                + "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">"
                                + "<soap:Header>"
                                + "<PassportCookie xmlns=\"http://www.hotmail.msn.com/ws/2004/09/oim/rsi\">"
                                + "<t>{t}</t>"
                                + "<p>{p}</p>"
                                + "</PassportCookie>"
                                + "</soap:Header><soap:Body>"
                                + "<DeleteMessages xmlns=\"http://www.hotmail.msn.com/ws/2004/09/oim/rsi\">"
                                + "<messageIds>"
                                + "<messageId>{MessageID}</messageId>"
                                + "</messageIds>"
                                + "</DeleteMessages>"
                                + "</soap:Body></soap:Envelope>";
                #endregion
            }
        }

        public static string SendOIMEnvelop
        {
            get
            {
                return
                #region Soap Envelope
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                            + "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">"
                            + "<soap:Header>"
                            + "<From memberName=\"{from_account}\" friendlyName=\"=?utf-8?B?{base64_nick}?=\" xml:lang=\"{lang}\" proxy=\"MSNMSGR\" xmlns=\"http://messenger.msn.com/ws/2004/09/oim/\" msnpVer=\"MSNP15\" buildVer=\"8.5.1288\"/>"
                            + "<To memberName=\"{to_account}\" xmlns=\"http://messenger.msn.com/ws/2004/09/oim/\"/>"
                            + "<Ticket passport=\"{oim_ticket}\" appid=\"{clientid}\" lockkey=\"{lockkey}\" xmlns=\"http://messenger.msn.com/ws/2004/09/oim/\"/>"
                            + "<Sequence xmlns=\"http://schemas.xmlsoap.org/ws/2003/03/rm\">"
                            + "<Identifier xmlns=\"http://schemas.xmlsoap.org/ws/2002/07/utility\">http://messenger.msn.com</Identifier>"
                            + "<MessageNumber>1</MessageNumber>"
                            + "</Sequence>"
                            + "</soap:Header>"
                            + "<soap:Body>"
                            + "<MessageType xmlns=\"http://messenger.msn.com/ws/2004/09/oim/\">text</MessageType>"
                            + "<Content xmlns=\"http://messenger.msn.com/ws/2004/09/oim/\">MIME-Version: 1.0\r\n"
                            + "Content-Type: text/plain; charset=UTF-8\r\n"
                            + "Content-Transfer-Encoding: base64\r\n"
                            + "X-OIM-Message-Type: OfflineMessage\r\n"
                            + "X-OIM-Run-Id: {{run_id}}\r\n"
                            + "X-OIM-Sequence-Num: {seq-num}\r\n"
                            + "\r\n"
                            + "{base64_msg}\r\n"
                            + "</Content>"
                            + "</soap:Body>"
                            + "</soap:Envelope>";
                #endregion
            }
        }

        public static string DeleteMemberByMemberShipIdEnvelop
        {
            get
            {
                return
                #region Soap Envelop
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                    + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                    + "<soap:Header>"
                    + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<ApplicationId>996CDE1E-AA53-4477-B943-2BE802EA6166</ApplicationId>"
                    + "<IsMigration>false</IsMigration>"
                    + "<PartnerScenario>BlockUnblock</PartnerScenario>"
                    + "<CacheKey>{cachekey}</CacheKey>"
                    + "</ABApplicationHeader>"
                    + "<ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                    + "<TicketToken>{ticket}</TicketToken>"
                    + "</ABAuthHeader></soap:Header>"
                    + "<soap:Body>"
                    + "<DeleteMember xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<serviceHandle>"
                    + "<Id>{messengerServiceId}</Id>"
                    + "<Type>Messenger</Type>"
                    + "<ForeignId></ForeignId>"
                    + "</serviceHandle>"
                    + "<memberships><Membership>"
                    + "<MemberRole>{list}</MemberRole>"
                    + "<Members>"
                    + "<Member xsi:type=\"{member_type}Member\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"
                    + "<Type>{member_type}</Type>"
                    + "<MembershipId>{membershipId}</MembershipId>"
                    + "<State>Accepted</State>"
                    + "</Member></Members></Membership>"
                    + "</memberships></DeleteMember></soap:Body>"
                    + "</soap:Envelope>";
                #endregion
            }
        }

        public static string AddMemberEnvelop
        {
            get
            {
                return
                #region Soap Envelop
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                    + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                    + "<soap:Header>"
                    + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<ApplicationId>996CDE1E-AA53-4477-B943-2BE802EA6166</ApplicationId>"
                    + "<IsMigration>false</IsMigration>"
                    + "<PartnerScenario>BlockUnblock</PartnerScenario>"
                    + "<BrandId>MSFT</BrandId>"
                    + "<CacheKey>{cachekey}</CacheKey>"
                    + "</ABApplicationHeader>"
                    + "<ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                    + "<TicketToken>{ticket}</TicketToken>"
                    + "</ABAuthHeader>"
                    + "</soap:Header><soap:Body>"
                    + "<AddMember xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<serviceHandle>"
                    + "<Id>{messengerServiceId}</Id>"
                    + "<Type>Messenger</Type>"
                    + "<ForeignId></ForeignId>"
                    + "</serviceHandle><memberships><Membership>"
                    + "<MemberRole>{list}</MemberRole>"
                    + "<Members>{member}</Members></Membership></memberships>"
                    + "</AddMember>"
                    + "</soap:Body></soap:Envelope>";
                #endregion
            }
        }

        public static string DeleteMemberByAccountEnvelop
        {
            get
            {
                return
                #region Soap Envelop
 "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                    + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">"
                    + "<soap:Header>"
                    + "<ABApplicationHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<ApplicationId>996CDE1E-AA53-4477-B943-2BE802EA6166</ApplicationId>"
                    + "<IsMigration>false</IsMigration>"
                    + "<PartnerScenario>BlockUnblock</PartnerScenario>"
                    + "<BrandId>MSFT</BrandId>"
                    + "<CacheKey>{cachekey}</CacheKey>"
                    + "</ABApplicationHeader>"
                    + "<ABAuthHeader xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<ManagedGroupRequest>false</ManagedGroupRequest>"
                    + "<TicketToken>{ticket}</TicketToken>"
                    + "</ABAuthHeader>"
                    + "</soap:Header><soap:Body>"
                    + "<DeleteMember xmlns=\"http://www.msn.com/webservices/AddressBook\">"
                    + "<serviceHandle>"
                    + "<Id>{messengerServiceId}</Id>"
                    + "<Type>Messenger</Type>"
                    + "<ForeignId></ForeignId>"
                    + "</serviceHandle><memberships><Membership>"
                    + "<MemberRole>{list}</MemberRole>"
                    + "<Members>{member}</Members></Membership></memberships>"
                    + "</DeleteMember>"
                    + "</soap:Body></soap:Envelope>";
                #endregion
            }
        }


        public static string PassportMember
        {
            get
            {
                return
                    "<Member xsi:type=\"PassportMember\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"
                    + "<Type>Passport</Type>"
                    + "<State>Accepted</State>"
                    + "<PassportName>{account}</PassportName>"
                    + "</Member>";

            }
        }

        public static string EmailMember
        {
            get
            {
                return
                    "<Member xsi:type=\"EmailMember\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"
                    + "<Type>Email</Type>"
                    + "<State>Accepted</State>"
                    + "<Email>{account}</Email>"
                    + "<Annotations>"
                    + "<Annotation>"
                    + "<Name>MSN.IM.BuddyType</Name>"
                    + "<Value>32:</Value>"
                    + "</Annotation>"
                    + "</Annotations>"
                    + "</Member>";
            }
        }

        public static string EmailMember_2
        {
            get
            {
                return
                    "<Member xsi:type=\"EmailMember\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"
                    + "<Type>Email</Type>"
                    + "<State>Accepted</State>"
                    + "<Email>{account}</Email>"
                    + "</Member>";
            }
        }
    }
}
