(function(){"use strict";var e={3149:function(e,t,n){var r,a=n(144),s=n(5069),i=n(3017),o=(n(6930),function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",[n("b-button",{directives:[{name:"b-modal",rawName:"v-b-modal.modal-add-printer",modifiers:{"modal-add-printer":!0}}],attrs:{disabled:e.disabled,size:"sm",variant:"success"}},[n("b-icon",{attrs:{icon:"plus"}}),e._v(" Add Printer ")],1),n("b-modal",{attrs:{id:"modal-add-printer",title:"Add Printer","ok-disabled":""==e.hostname},on:{ok:e.ok}},[n("p",[e._v("Please enter the hostname of the new printer:")]),n("b-form-input",{attrs:{placeholder:"Hostname or IP address",autofocus:""},on:{keyup:function(t){return!t.type.indexOf("key")&&e._k(t.keyCode,"enter",13,t.key,"Enter")?null:e.ok.apply(null,arguments)}},model:{value:e.hostname,callback:function(t){e.hostname=t},expression:"hostname"}})],1)],1)}),l=[],c=(n(1703),n(3796));(function(e){e["disconnected"]="disconnected",e["starting"]="starting",e["updating"]="updating",e["off"]="off",e["Halted"]="halted",e["Pausing"]="pausing",e["paused"]="paused",e["resuming"]="resuming",e["cancelling"]="cancelling",e["processing"]="processing",e["simulating"]="processing",e["busy"]="busy",e["changingTool"]="changingTool",e["idle"]="idle"})(r||(r={}));class u{constructor(){(0,c.Z)(this,"AbsoluteFilename",""),(0,c.Z)(this,"Filename",""),(0,c.Z)(this,"Hostname",""),(0,c.Z)(this,"TimeCreated",null),(0,c.Z)(this,"ProgressText",null),(0,c.Z)(this,"Progress",null),(0,c.Z)(this,"Paused",!1),(0,c.Z)(this,"Cancelled",!1),(0,c.Z)(this,"TimeLeft",null),(0,c.Z)(this,"TimeCompleted",null)}static reviver(e,t){return"TimeCreated"!==e&&"TimeCompleted"!==e||"string"!==typeof t?t:new Date(t)}}const d=`${location.protocol}//${location.host}/`,p=4e3;function m(e,t,n=null,r=null,a=p){let s=d+t;if(n){let e=!1;for(const t in n)s+=(e?"&":"?")+t+"="+encodeURIComponent(n[t]),e=!0}const i=new XMLHttpRequest;return i.open(e,s),i.responseType="text",i.setRequestHeader("Content-Type","application/json"),i.timeout=a,new Promise(((e,t)=>{i.onload=function(){if(i.status>=200&&i.status<300)try{i.responseText?e(i.responseText):e(null)}catch(n){t(n)}else 0!==i.status?t(new Error(`Server returned HTTP code ${i.status} ${i.statusText}`)):t(new Error("HTTP request failed"))},i.onabort=function(){t(new Error("Request aborted"))},i.onerror=function(){t(new Error("HTTP request failed"))},i.ontimeout=function(){t(new Error("HTTP request timed out"))},i.send(r)}))}async function f(){const e=await m("GET","printFarm/queue");return e?JSON.parse(e,u.reviver):[]}async function b(e,t){const n=t instanceof Blob?t:new Blob([t]);await m("PUT","printFarm/job",{filename:e},n,0)}async function h(e){await m("POST","printFarm/pause",{index:e.toString()})}async function g(e){await m("POST","printFarm/resume",{index:e.toString()})}async function y(e){await m("POST","printFarm/cancel",{index:e.toString()})}async function v(e){await m("POST","printFarm/repeat",{index:e.toString()})}async function w(e){return m("DELETE","printFarm/job",{index:e.toString()})}async function T(){await m("POST","printFarm/cleanUp")}async function P(){const e=await m("GET","printFarm/printers");return e?JSON.parse(e):[]}async function k(e){await m("PUT","printFarm/printer",{hostname:e})}async function F(e){await m("POST","printFarm/suspendPrinter",{hostname:e})}async function C(e){return m("POST","printFarm/resumePrinter",{hostname:e})}async function x(e){return m("DELETE","printFarm/printer",{hostname:e})}var S={props:{disabled:{default:!1,type:Boolean}},data(){return{hostname:""}},methods:{async ok(){this.$bvModal.hide("modal-add-printer");try{await k(this.hostname),this.hostname=""}catch(e){alert(`Failed to add printer:\n\n${e.message}`)}}}},_=S,j=n(3736),O=(0,j.Z)(_,o,l,!1,null,null,null),H=O.exports,$=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("b-badge",{attrs:{variant:e.statusVariant}},[e._v(" "+e._s(e.statusText)+" ")])},E=[],Z={props:{status:{required:!0,type:String}},computed:{statusText(){let e=this.status;return this.status?"processing"===this.status&&(e="printing"):e="unknown",e[0].toUpperCase()+e.substring(1)},statusVariant(){switch(this.status){case"disconnected":return"danger";case"starting":return"info";case"updating":return"primary";case"off":return"danger";case"halted":return"danger";case"pausing":return"warning";case"paused":return"warning";case"resuming":return"secondary";case"processing":return"success";case"simulating":return"success";case"busy":return"warning";case"changingTool":return"primary";case"idle":return"info";default:return"dark"}}}},M=Z,N=(0,j.Z)(M,$,E,!1,null,null,null),J=N.exports,z=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("b-button",{attrs:{disabled:e.disabled,loading:e.isBusy,size:"sm",variant:"primary"},on:{click:e.chooseFile}},[n("b-icon",{attrs:{icon:"cloud-upload"}}),e._v(" Upload File "),n("input",{ref:"fileInput",attrs:{type:"file",accept:".g,.gcode,.gc,.gco,.nc,.ngc,.tap",hidden:"",multiple:""},on:{change:e.fileSelected}})],1)},L=[],D={props:{disabled:{default:!1,type:Boolean}},data(){return{isBusy:!1}},methods:{chooseFile(){this.isBusy||this.$refs.fileInput.click()},async fileSelected(e){this.isBusy=!0;try{for(let t=0;t<e.target.files.length;t++)await b(e.target.files[t].name,e.target.files[t])}catch(e){alert("Upload failed!\n\n"+e.message)}e.target.value="",this.isBusy=!1}}},R=D,U=(0,j.Z)(R,z,L,!1,null,null,null),B=U.exports;a["default"].component("add-printer-button",H),a["default"].component("status-label",J),a["default"].component("upload-button",B);var q=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",{attrs:{id:"app"}},[n("b-container",{staticClass:"my-3",attrs:{fluid:""}},[n("h1",{staticClass:"mb-4 text-center"},[e._v(" Duet3D Print Farm Overview ")]),n("b-alert",{attrs:{show:!!e.errorMessage,variant:null!=e.errorMessage&&e.errorMessage.startsWith("Error")?"danger":"warning"}},[n("b-icon",{staticClass:"mr-1",attrs:{icon:"exclamation-triangle"}}),e._v(" "+e._s(e.errorMessage)+" ")],1),n("b-row",[n("b-col",{attrs:{cols:"8"}},[n("b-card",{attrs:{"no-body":""},scopedSlots:e._u([{key:"header",fn:function(){return[n("span",[n("b-icon",{attrs:{icon:"card-list"}}),e._v(" Job Queue ")],1),n("div",[n("upload-button",{attrs:{disabled:!!e.errorMessage}}),n("b-button",{directives:[{name:"show",rawName:"v-show",value:e.canClean,expression:"canClean"}],staticClass:"ml-2",attrs:{size:"sm",variant:"info"},on:{click:e.cleanUp}},[n("b-icon",{attrs:{icon:"filter-left"}}),e._v(" Clean Up ")],1)],1)]},proxy:!0}])},[n("b-alert",{staticClass:"mb-0",attrs:{show:0===e.jobs.length,variant:"info"}},[n("b-icon",{staticClass:"mr-1",attrs:{icon:"info-circle"}}),e._v(" No Jobs available ")],1),n("b-table",{directives:[{name:"show",rawName:"v-show",value:e.jobs.length>0,expression:"jobs.length > 0"}],staticClass:"mb-0 job-table",attrs:{striped:"",hover:"",fields:e.jobFields,items:e.jobs,"no-provider-paging":"","current-page":e.currentJobPage,"per-page":10},scopedSlots:e._u([{key:"cell(Filename)",fn:function(t){var r=t.item;return[n("b-icon",{attrs:{icon:e.getJobIcon(r),"icon-props":{fontScale:2}}}),e._v(" "+e._s(r.Filename)+" ")]}},{key:"cell(Hostname)",fn:function(t){var n=t.item;return[e._v(" "+e._s(e.getPrinterName(n.Hostname))+" ")]}},{key:"cell(Progress)",fn:function(t){var r=t.item;return[r.ProgressText?n("span",{domProps:{textContent:e._s(r.ProgressText)}}):null!==r.Progress||r.TimeCompleted?n("b-progress",{attrs:{max:1,"show-progress":"",animated:!r.TimeCompleted,variant:e.getJobProgressVariant(r)}},[n("b-progress-bar",{attrs:{value:r.TimeCompleted?1:r.Progress,label:(100*(r.TimeCompleted?1:r.Progress)).toFixed(1)+" %"}})],1):e._e()]}},{key:"cell(Time)",fn:function(t){var n=t.item;return[e._v(" "+e._s(e.formatTime(n))+" ")]}},{key:"cell(ResumeRepeat)",fn:function(t){var r=t.item,a=t.index;return[r.Paused?n("b-button",{attrs:{size:"sm",variant:"success"},on:{click:function(t){return e.resumeFile(a)}}},[n("b-icon",{attrs:{icon:"play-fill"}})],1):r.TimeCompleted?n("b-button",{attrs:{size:"sm",variant:"primary"},on:{click:function(t){return e.repeatFile(a)}}},[n("b-icon",{attrs:{icon:"arrow-repeat"}})],1):e._e()]}},{key:"cell(PauseCancelDelete)",fn:function(t){var r=t.item,a=t.index;return[r.Paused?n("b-button",{attrs:{size:"sm",variant:"danger"},on:{click:function(t){return e.cancelFile(a)}}},[n("b-icon",{attrs:{icon:"stop-fill"}})],1):!r.Hostname||r.TimeCompleted?n("b-button",{attrs:{size:"sm",variant:"danger"},on:{click:function(t){return e.deleteFile(a)}}},[n("b-icon",{attrs:{icon:"trash"}})],1):null!==r.Progress?n("b-button",{attrs:{size:"sm",variant:"warning",disabled:!!r.ProgressText},on:{click:function(t){return e.pauseFile(a)}}},[n("b-icon",{attrs:{icon:"pause"}})],1):e._e()]}}])}),n("b-pagination",{directives:[{name:"show",rawName:"v-show",value:e.jobs.length>10,expression:"jobs.length > 10"}],staticClass:"my-0",attrs:{"total-rows":e.jobs.length,"per-page":10,align:"fill",size:"sm"},model:{value:e.currentJobPage,callback:function(t){e.currentJobPage=t},expression:"currentJobPage"}})],1)],1),n("b-col",{staticClass:"pl-0",attrs:{cols:"4"}},[n("b-card",{attrs:{"no-body":""},scopedSlots:e._u([{key:"header",fn:function(){return[n("span",[n("b-icon",{attrs:{icon:"printer"}}),e._v(" Printer Management ")],1),n("add-printer-button",{attrs:{disabled:!!e.errorMessage}})]},proxy:!0}])},[n("b-alert",{staticClass:"mb-0",attrs:{show:0===e.printers.length,variant:"warning"}},[n("b-icon",{staticClass:"mr-1",attrs:{icon:"exclamation-triangle"}}),e._v(" No Printers available ")],1),n("b-table",{directives:[{name:"show",rawName:"v-show",value:e.printers.length>0,expression:"printers.length > 0"}],staticClass:"mb-0 printer-table",attrs:{striped:"",hover:"",fields:e.printerFields,items:e.printers},scopedSlots:e._u([{key:"cell(Name)",fn:function(t){var r=t.item;return[n("b-icon",{attrs:{icon:e.getPrinterIcon(r)}}),e._v(" "+e._s(r.Name)+" "),n("status-label",{staticClass:"ml-1",attrs:{status:r.Status}})]}},{key:"cell(Online)",fn:function(t){var n=t.item;return[e._v(" "+e._s((n.Online?"Yes":"No")+" "+(n.Suspended?" (suspended)":""))+" ")]}},{key:"cell(SuspendResume)",fn:function(t){var r=t.item;return[r.Suspended?n("b-button",{attrs:{size:"sm",variant:"success"},on:{click:function(t){return e.resumePrinter(r.Hostname)}}},[n("b-icon",{attrs:{icon:"play-fill"}})],1):n("b-button",{attrs:{size:"sm",variant:"warning"},on:{click:function(t){return e.suspendPrinter(r.Hostname)}}},[n("b-icon",{attrs:{icon:"pause"}})],1)]}},{key:"cell(Delete)",fn:function(t){var r=t.item;return[n("b-button",{attrs:{size:"sm",variant:"danger"},on:{click:function(t){return e.deletePrinter(r.Hostname)}}},[n("b-icon",{attrs:{icon:"trash"}})],1)]}}])})],1)],1)],1)],1)],1)},I=[],A=n(655),V=n(1929);function G(e,t=!1){if(null==e||isNaN(e))return"n/a";e=Math.round(e),e<0&&(e=0);const n=[];if(e>=3600){const t=Math.floor(e/3600);t>0&&(n.push(t+"h"),e%=3600)}if(e>=60){const r=Math.floor(e/60);r>0&&(n.push((e>9||!t?r:"0"+r)+"m"),e%=60)}return e=Math.floor(e),n.push((e>9||!t?e:"0"+e)+"s"),n.join(" ")}let X=class extends V.w3{constructor(...e){super(...e),(0,c.Z)(this,"errorMessage","Attempting to connect..."),(0,c.Z)(this,"jobFields",[{key:"Filename"},{key:"TimeCreated",formatter:e=>e?new Date(e).toLocaleString():"n/a"},{key:"Hostname",label:"Printer"},{key:"Progress"},{key:"Time",label:"Time Left / Completed"},{key:"ResumeRepeat",label:""},{key:"PauseCancelDelete",label:""}]),(0,c.Z)(this,"jobs",[]),(0,c.Z)(this,"currentJobPage",1),(0,c.Z)(this,"printerFields",[{key:"Name",sortable:!0},{key:"Hostname",sortable:!0},{key:"SuspendResume",label:"",sortable:!1},{key:"Delete",label:"",sortable:!1}]),(0,c.Z)(this,"printers",[])}get canClean(){return this.jobs.some((e=>null!==e.TimeCompleted))}mounted(){this.updateLoop()}getJobIcon(e){return e.Paused?"pause":e.TimeCompleted?e.Cancelled?"x":"check":e.Hostname?this.printers.some((t=>t.Hostname===e.Hostname&&"pausing"===t.Status||"paused"===t.Status||"resuming"===t.Status||"cancelling"===t.Status))?"pause":"play-fill":"asterisk"}getPrinterName(e){const t=this.printers.find((t=>t.Hostname===e));return t?t.Name:e}getJobProgressVariant(e){return e.TimeCompleted?e.Cancelled?"danger":"success":!e.Hostname||e.Paused||this.printers.some((t=>t.Hostname===e.Hostname&&("pausing"===t.Status||"paused"===t.Status||"resuming"===t.Status||"cancelling"===t.Status)))?"warning":"primary"}formatTime(e){return e.TimeCompleted?new Date(e.TimeCompleted).toLocaleString():e.TimeLeft?`${G(e.TimeLeft)} remaining`:""}getPrinterIcon(e){return e.Online?"check":"x"}async updateLoop(){try{this.jobs=await f(),this.printers=await P(),this.errorMessage=null}catch(e){this.jobs=this.printers=[],this.errorMessage=e}setTimeout(this.updateLoop,1e3)}async cleanUp(){try{await T(),this.jobs=await f()}catch(e){alert(`Failed to clean up!\n\n${e}`)}}async pauseFile(e){try{await h(e),this.jobs=await f()}catch(t){alert(`Failed to pause file!\n\n${t}`)}}async resumeFile(e){try{await g(e),this.jobs=await f()}catch(t){alert(`Failed to resume file!\n\n${t}`)}}async cancelFile(e){try{await y(e),this.jobs=await f()}catch(t){alert(`Failed to cancel file!\n\n${t}`)}}async repeatFile(e){try{await v(e),this.jobs=await f()}catch(t){alert(`Failed to repeat file!\n\n${t}`)}}async deleteFile(e){try{await w(e),this.jobs=await f()}catch(t){alert(`Failed to delete file!\n\n${t}`)}}async suspendPrinter(e){try{await F(e),this.printers=await P(),this.printers.some((t=>t.Hostname===e&&null!==t.JobFile))&&alert("This printer will be suspended as soon as the current print job has finished")}catch(t){alert(`Failed to suspend printer!\n\n${t}`)}}async resumePrinter(e){try{await C(e),this.printers=await P()}catch(t){alert(`Failed to resume printer!\n\n${t}`)}}async deletePrinter(e){try{await x(e),this.printers=await P()}catch(t){alert(`Failed to delete printer!\n\n${t}`)}}};X=(0,A.gn)([V.wA],X);var Q=X,W=Q,Y=(0,j.Z)(W,q,I,!1,null,"2da7440d",null),K=Y.exports;a["default"].config.productionTip=!1,a["default"].use(s.XG7),a["default"].use(i.X),new a["default"]({el:"#app",render:e=>e(K)})}},t={};function n(r){var a=t[r];if(void 0!==a)return a.exports;var s=t[r]={exports:{}};return e[r](s,s.exports,n),s.exports}n.m=e,function(){var e=[];n.O=function(t,r,a,s){if(!r){var i=1/0;for(u=0;u<e.length;u++){r=e[u][0],a=e[u][1],s=e[u][2];for(var o=!0,l=0;l<r.length;l++)(!1&s||i>=s)&&Object.keys(n.O).every((function(e){return n.O[e](r[l])}))?r.splice(l--,1):(o=!1,s<i&&(i=s));if(o){e.splice(u--,1);var c=a();void 0!==c&&(t=c)}}return t}s=s||0;for(var u=e.length;u>0&&e[u-1][2]>s;u--)e[u]=e[u-1];e[u]=[r,a,s]}}(),function(){n.d=function(e,t){for(var r in t)n.o(t,r)&&!n.o(e,r)&&Object.defineProperty(e,r,{enumerable:!0,get:t[r]})}}(),function(){n.g=function(){if("object"===typeof globalThis)return globalThis;try{return this||new Function("return this")()}catch(e){if("object"===typeof window)return window}}()}(),function(){n.o=function(e,t){return Object.prototype.hasOwnProperty.call(e,t)}}(),function(){n.r=function(e){"undefined"!==typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(e,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(e,"__esModule",{value:!0})}}(),function(){var e={143:0};n.O.j=function(t){return 0===e[t]};var t=function(t,r){var a,s,i=r[0],o=r[1],l=r[2],c=0;if(i.some((function(t){return 0!==e[t]}))){for(a in o)n.o(o,a)&&(n.m[a]=o[a]);if(l)var u=l(n)}for(t&&t(r);c<i.length;c++)s=i[c],n.o(e,s)&&e[s]&&e[s][0](),e[s]=0;return n.O(u)},r=self["webpackChunkduetprintfarmui"]=self["webpackChunkduetprintfarmui"]||[];r.forEach(t.bind(null,0)),r.push=t.bind(null,r.push.bind(r))}();var r=n.O(void 0,[998],(function(){return n(3149)}));r=n.O(r)})();
//# sourceMappingURL=app.baf56cf9.js.map