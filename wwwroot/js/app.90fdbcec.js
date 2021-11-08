(function(e){function t(t){for(var r,i,o=t[0],c=t[1],u=t[2],p=0,d=[];p<o.length;p++)i=o[p],Object.prototype.hasOwnProperty.call(a,i)&&a[i]&&d.push(a[i][0]),a[i]=0;for(r in c)Object.prototype.hasOwnProperty.call(c,r)&&(e[r]=c[r]);l&&l(t);while(d.length)d.shift()();return s.push.apply(s,u||[]),n()}function n(){for(var e,t=0;t<s.length;t++){for(var n=s[t],r=!0,o=1;o<n.length;o++){var c=n[o];0!==a[c]&&(r=!1)}r&&(s.splice(t--,1),e=i(i.s=n[0]))}return e}var r={},a={app:0},s=[];function i(t){if(r[t])return r[t].exports;var n=r[t]={i:t,l:!1,exports:{}};return e[t].call(n.exports,n,n.exports,i),n.l=!0,n.exports}i.m=e,i.c=r,i.d=function(e,t,n){i.o(e,t)||Object.defineProperty(e,t,{enumerable:!0,get:n})},i.r=function(e){"undefined"!==typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(e,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(e,"__esModule",{value:!0})},i.t=function(e,t){if(1&t&&(e=i(e)),8&t)return e;if(4&t&&"object"===typeof e&&e&&e.__esModule)return e;var n=Object.create(null);if(i.r(n),Object.defineProperty(n,"default",{enumerable:!0,value:e}),2&t&&"string"!=typeof e)for(var r in e)i.d(n,r,function(t){return e[t]}.bind(null,r));return n},i.n=function(e){var t=e&&e.__esModule?function(){return e["default"]}:function(){return e};return i.d(t,"a",t),t},i.o=function(e,t){return Object.prototype.hasOwnProperty.call(e,t)},i.p="/";var o=window["webpackJsonp"]=window["webpackJsonp"]||[],c=o.push.bind(o);o.push=t,o=o.slice();for(var u=0;u<o.length;u++)t(o[u]);var l=c;s.push([0,"chunk-vendors"]),n()})({0:function(e,t,n){e.exports=n("56d7")},"452c":function(e,t,n){},"56d7":function(e,t,n){"use strict";n.r(t);n("e260"),n("e6cf"),n("cca6"),n("a79d");var r=n("2b0e"),a=n("5f5b"),s=n("b1e0"),i=(n("ab8b"),n("2dd8"),function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",[n("b-button",{directives:[{name:"b-modal",rawName:"v-b-modal.modal-add-printer",modifiers:{"modal-add-printer":!0}}],attrs:{disabled:e.disabled,size:"sm",variant:"success"}},[n("b-icon",{attrs:{icon:"plus"}}),e._v(" Add Printer ")],1),n("b-modal",{attrs:{id:"modal-add-printer",title:"Add Printer","ok-disabled":""==e.hostname},on:{ok:e.ok}},[n("p",[e._v("Please enter the hostname of the new printer:")]),n("b-form-input",{attrs:{placeholder:"Hostname or IP address",autofocus:""},on:{keyup:function(t){return!t.type.indexOf("key")&&e._k(t.keyCode,"enter",13,t.key,"Enter")?null:e.ok.apply(null,arguments)}},model:{value:e.hostname,callback:function(t){e.hostname=t},expression:"hostname"}})],1)],1)}),o=[],c=n("1da1"),u=(n("96cf"),n("99af"),n("d3b7"),4e3),l="".concat(location.protocol,"//").concat(location.host,"/");function p(e,t){var n=arguments.length>2&&void 0!==arguments[2]?arguments[2]:null,r=arguments.length>3&&void 0!==arguments[3]?arguments[3]:null,a=arguments.length>4&&void 0!==arguments[4]?arguments[4]:u,s=l+t;if(n){var i=!1;for(var o in n)s+=(i?"&":"?")+o+"="+encodeURIComponent(n[o]),i=!0}var c=new XMLHttpRequest;return c.open(e,s),c.responseType="text",c.setRequestHeader("Content-Type","application/json"),c.timeout=a,new Promise((function(e,t){c.onload=function(){if(c.status>=200&&c.status<300)try{c.responseText?e(JSON.parse(c.responseText)):e(null)}catch(n){t(n)}else 0!==c.status?t(new Error("Server returned HTTP code ".concat(c.status," ").concat(c.statusText))):t(new Error("HTTP request failed"))},c.onabort=function(){t(new Error("Request aborted"))},c.onerror=function(){t(new Error("HTTP request failed"))},c.ontimeout=function(){t(new Error("HTTP request timed out"))},c.send(r)}))}function d(){return p("GET","printFarm/queue")}function m(e,t){var n=t instanceof Blob?t:new Blob([t]);return p("PUT","printFarm/job",{filename:e},n,0)}function f(e){return p("POST","printFarm/pause",{index:e})}function b(e){return p("POST","printFarm/resume",{index:e})}function v(e){return p("POST","printFarm/cancel",{index:e})}function g(e){return p("POST","printFarm/repeat",{index:e})}function h(e){return p("DELETE","printFarm/job",{index:e})}function x(){return p("POST","printFarm/cleanUp")}function w(){return p("GET","printFarm/printers")}function k(e){return p("PUT","printFarm/printer",{hostname:e})}function y(e){return p("POST","printFarm/suspendPrinter",{hostname:e})}function P(e){return p("POST","printFarm/resumePrinter",{hostname:e})}function T(e){return p("DELETE","printFarm/printer",{hostname:e})}var j={props:{disabled:{default:!1,type:Boolean}},data:function(){return{hostname:""}},methods:{ok:function(){var e=this;return Object(c["a"])(regeneratorRuntime.mark((function t(){return regeneratorRuntime.wrap((function(t){while(1)switch(t.prev=t.next){case 0:return e.$bvModal.hide("modal-add-printer"),t.prev=1,t.next=4,k(e.hostname);case 4:e.hostname="",t.next=10;break;case 7:t.prev=7,t.t0=t["catch"](1),alert("Failed to add printer:\n\n".concat(t.t0.message));case 10:case"end":return t.stop()}}),t,null,[[1,7]])})))()}}},F=j,_=n("2877"),O=Object(_["a"])(F,i,o,!1,null,null,null),C=O.exports,S=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("b-badge",{attrs:{variant:e.statusVariant}},[e._v(" "+e._s(e.statusText)+" ")])},R=[],H={props:{status:{required:!0,type:String}},computed:{statusText:function(){var e=this.status;return this.status?"processing"===this.status&&(e="printing"):e="unknown",e[0].toUpperCase()+e.substring(1)},statusVariant:function(){switch(this.status){case"disconnected":return"danger";case"starting":return"info";case"updating":return"primary";case"off":return"danger";case"halted":return"danger";case"pausing":return"warning";case"paused":return"warning";case"resuming":return"secondary";case"processing":return"success";case"simulating":return"success";case"busy":return"warning";case"changingTool":return"primary";case"idle":return"info";default:return"dark"}}}},E=H,M=Object(_["a"])(E,S,R,!1,null,null,null),N=M.exports,J=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("b-button",{attrs:{disabled:e.disabled,loading:e.isBusy,size:"sm",variant:"primary"},on:{click:e.chooseFile}},[n("b-icon",{attrs:{icon:"cloud-upload"}}),e._v(" Upload File "),n("input",{ref:"fileInput",attrs:{type:"file",accept:".g,.gcode,.gc,.gco,.nc,.ngc,.tap",hidden:"",multiple:""},on:{change:e.fileSelected}})],1)},z=[],L=(n("b0c0"),{props:{disabled:{default:!1,type:Boolean}},data:function(){return{isBusy:!1}},methods:{chooseFile:function(){this.isBusy||this.$refs.fileInput.click()},fileSelected:function(e){var t=this;return Object(c["a"])(regeneratorRuntime.mark((function n(){var r;return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:t.isBusy=!0,n.prev=1,r=0;case 3:if(!(r<e.target.files.length)){n.next=9;break}return n.next=6,m(e.target.files[r].name,e.target.files[r]);case 6:r++,n.next=3;break;case 9:n.next=14;break;case 11:n.prev=11,n.t0=n["catch"](1),alert("Upload failed!\n\n"+n.t0.message);case 14:e.target.value="",t.isBusy=!1;case 16:case"end":return n.stop()}}),n,null,[[1,11]])})))()}}}),D=L,U=Object(_["a"])(D,J,z,!1,null,null,null),B=U.exports;r["default"].component("add-printer-button",C),r["default"].component("status-label",N),r["default"].component("upload-button",B);var q=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",{attrs:{id:"app"}},[n("b-container",{staticClass:"mt-3"},[n("h1",{staticClass:"mb-4 text-center"},[e._v(" Duet3D Print Farm Overview ")]),n("b-alert",{attrs:{show:!!e.errorMessage,variant:"danger"}},[n("b-icon",{staticClass:"mr-1",attrs:{icon:"exclamation-triangle"}}),e._v(" "+e._s(e.errorMessage)+" ")],1),n("b-card",{attrs:{"no-body":""},scopedSlots:e._u([{key:"header",fn:function(){return[n("span",[n("b-icon",{attrs:{icon:"card-list"}}),e._v(" Job Queue ")],1),n("div",[n("upload-button",{attrs:{disabled:!!e.errorMessage}}),n("b-button",{directives:[{name:"show",rawName:"v-show",value:e.canClean,expression:"canClean"}],staticClass:"ml-2",attrs:{size:"sm",variant:"info"},on:{click:e.cleanUp}},[n("b-icon",{attrs:{icon:"filter-left"}}),e._v(" Clean Up ")],1)],1)]},proxy:!0}])},[n("b-alert",{staticClass:"mb-0",attrs:{show:0===e.jobs.length,variant:"info"}},[n("b-icon",{staticClass:"mr-1",attrs:{icon:"info-circle"}}),e._v(" No Jobs available ")],1),n("b-table",{directives:[{name:"show",rawName:"v-show",value:e.jobs.length>0,expression:"jobs.length > 0"}],staticClass:"mb-0 job-table",attrs:{striped:"",hover:"",fields:e.jobFields,items:e.jobs,"no-provider-paging":"","current-page":e.currentJobPage,"per-page":10},scopedSlots:e._u([{key:"cell(Filename)",fn:function(t){var r=t.item;return[n("b-icon",{attrs:{icon:e.getJobIcon(r),"icon-props":{fontScale:2}}}),e._v(" "+e._s(r.Filename)+" ")]}},{key:"cell(Hostname)",fn:function(t){var n=t.item;return[e._v(" "+e._s(e.getPrinterName(n.Hostname))+" ")]}},{key:"cell(Progress)",fn:function(t){var r=t.item;return[r.ProgressText?n("span",{domProps:{textContent:e._s(r.ProgressText)}}):null!==r.Progress||r.TimeCompleted?n("b-progress",{attrs:{max:1,"show-progress":"",animated:!r.TimeCompleted,variant:e.getJobProgressVariant(r)}},[n("b-progress-bar",{attrs:{value:r.TimeCompleted?1:r.Progress,label:(100*(r.TimeCompleted?1:r.Progress)).toFixed(1)+" %"}})],1):e._e()]}},{key:"cell(Time)",fn:function(t){var n=t.item;return[e._v(" "+e._s(e.formatTime(n))+" ")]}},{key:"cell(ResumeRepeat)",fn:function(t){var r=t.item,a=t.index;return[r.Paused?n("b-button",{attrs:{size:"sm",variant:"success"},on:{click:function(t){return e.resumeFile(a)}}},[n("b-icon",{attrs:{icon:"play-fill"}})],1):r.TimeCompleted?n("b-button",{attrs:{size:"sm",variant:"primary"},on:{click:function(t){return e.repeatFile(a)}}},[n("b-icon",{attrs:{icon:"arrow-repeat"}})],1):e._e()]}},{key:"cell(PauseCancelDelete)",fn:function(t){var r=t.item,a=t.index;return[r.Paused?n("b-button",{attrs:{size:"sm",variant:"danger"},on:{click:function(t){return e.cancelFile(a)}}},[n("b-icon",{attrs:{icon:"stop-fill"}})],1):!r.Hostname||r.TimeCompleted?n("b-button",{attrs:{size:"sm",variant:"danger"},on:{click:function(t){return e.deleteFile(a)}}},[n("b-icon",{attrs:{icon:"trash"}})],1):null!==r.Progress?n("b-button",{attrs:{size:"sm",variant:"warning",disabled:!!r.ProgressText},on:{click:function(t){return e.pauseFile(a)}}},[n("b-icon",{attrs:{icon:"pause"}})],1):e._e()]}}])}),n("b-pagination",{directives:[{name:"show",rawName:"v-show",value:e.jobs.length>10,expression:"jobs.length > 10"}],staticClass:"my-0",attrs:{"total-rows":e.jobs.length,"per-page":10,align:"fill",size:"sm"},model:{value:e.currentJobPage,callback:function(t){e.currentJobPage=t},expression:"currentJobPage"}})],1),n("b-card",{staticClass:"my-3",attrs:{"no-body":""},scopedSlots:e._u([{key:"header",fn:function(){return[n("span",[n("b-icon",{attrs:{icon:"printer"}}),e._v(" Printer Management ")],1),n("add-printer-button",{attrs:{disabled:!!e.errorMessage}})]},proxy:!0}])},[n("b-alert",{staticClass:"mb-0",attrs:{show:0===e.printers.length,variant:"warning"}},[n("b-icon",{staticClass:"mr-1",attrs:{icon:"exclamation-triangle"}}),e._v(" No Printers available ")],1),n("b-table",{directives:[{name:"show",rawName:"v-show",value:e.printers.length>0,expression:"printers.length > 0"}],staticClass:"mb-0 printer-table",attrs:{striped:"",hover:"",fields:e.printerFields,items:e.printers},scopedSlots:e._u([{key:"cell(Name)",fn:function(t){var r=t.item;return[n("b-icon",{attrs:{icon:e.getPrinterIcon(r)}}),e._v(" "+e._s(r.Name)+" "),n("status-label",{staticClass:"ml-1",attrs:{status:r.Status}})]}},{key:"cell(Online)",fn:function(t){var n=t.item;return[e._v(" "+e._s((n.Online?"Yes":"No")+" "+(n.Suspended?" (suspended)":""))+" ")]}},{key:"cell(SuspendResume)",fn:function(t){var r=t.item;return[r.Suspended?n("b-button",{attrs:{size:"sm",variant:"success"},on:{click:function(t){return e.resumePrinter(r.Hostname)}}},[n("b-icon",{attrs:{icon:"play-fill"}})],1):n("b-button",{attrs:{size:"sm",variant:"warning"},on:{click:function(t){return e.suspendPrinter(r.Hostname)}}},[n("b-icon",{attrs:{icon:"pause"}})],1)]}},{key:"cell(Delete)",fn:function(t){var r=t.item;return[n("b-button",{attrs:{size:"sm",variant:"danger"},on:{click:function(t){return e.deletePrinter(r.Hostname)}}},[n("b-icon",{attrs:{icon:"trash"}})],1)]}}])})],1)],1)],1)},I=[];n("7db0"),n("25f0"),n("b680"),n("a15b");function $(e){var t=arguments.length>1&&void 0!==arguments[1]&&arguments[1];if(null===e||isNaN(e))return"n/a";e=Math.round(e),e<0&&(e=0);var n,r=[];return e>=3600&&(n=Math.floor(e/3600),n>0&&(r.push(n+"h"),e%=3600)),e>=60&&(n=Math.floor(e/60),n>0&&(r.push((e>9||!t?n:"0"+n)+"m"),e%=60)),e=e.toFixed(0),r.push((e>9||!t?e:"0"+e)+"s"),r.join(" ")}var V={computed:{canClean:function(){return this.jobs.some((function(e){return null!==e.TimeCompleted}))}},data:function(){return{errorMessage:null,jobFields:[{key:"Filename"},{key:"TimeCreated",formatter:function(e){return e?new Date(e).toLocaleString():"n/a"}},{key:"Hostname",label:"Printer"},{key:"Progress"},{key:"Time",label:"Time Left / Completed"},{key:"ResumeRepeat",label:""},{key:"PauseCancelDelete",label:""}],jobs:[],currentJobPage:1,printerFields:[{key:"Name",sortable:!0},{key:"Hostname",sortable:!0},{key:"Online",sortable:!0},{key:"JobFile",formatter:function(e){return e||"none"},sortable:!0},{key:"SuspendResume",label:"",sortable:!1},{key:"Delete",label:"",sortable:!1}],printers:[]}},mounted:function(){this.updateLoop()},methods:{getJobIcon:function(e){return e.Paused?"pause":e.TimeCompleted?e.Cancelled?"x":"check":e.Hostname?this.printers.some((function(t){return t.Hostname===e.Hostname&&"pausing"===t.Status||"paused"===t.Status||"resuming"===t.Status||"cancelling"===t.Status}))?"pause":"play-fill":"asterisk"},getPrinterName:function(e){var t=this.printers.find((function(t){return t.Hostname===e}));return t?t.Name:e},getJobProgressVariant:function(e){return e.TimeCompleted?e.Cancelled?"danger":"success":!e.Hostname||e.Paused||this.printers.some((function(t){return t.Hostname===e.Hostname&&("pausing"===t.Status||"paused"===t.Status||"resuming"===t.Status||"cancelling"===t.Status)}))?"warning":"primary"},formatTime:function(e){return e.TimeCompleted?new Date(e.TimeCompleted).toLocaleString():e.TimeLeft?"".concat($(e.TimeLeft)," remaining"):""},getPrinterIcon:function(e){return e.Online?"check":"x"},updateLoop:function(){var e=this;return Object(c["a"])(regeneratorRuntime.mark((function t(){return regeneratorRuntime.wrap((function(t){while(1)switch(t.prev=t.next){case 0:return t.prev=0,t.next=3,d();case 3:return e.jobs=t.sent,t.next=6,w();case 6:e.printers=t.sent,e.errorMessage=null,t.next=14;break;case 10:t.prev=10,t.t0=t["catch"](0),e.jobs=e.printers=[],e.errorMessage=t.t0.toString();case 14:setTimeout(e.updateLoop,1e3);case 15:case"end":return t.stop()}}),t,null,[[0,10]])})))()},cleanUp:function(){var e=this;return Object(c["a"])(regeneratorRuntime.mark((function t(){return regeneratorRuntime.wrap((function(t){while(1)switch(t.prev=t.next){case 0:return t.prev=0,t.next=3,x();case 3:return t.next=5,d();case 5:e.jobs=t.sent,t.next=11;break;case 8:t.prev=8,t.t0=t["catch"](0),alert("Failed to clean up!\n\n".concat(t.t0.message));case 11:case"end":return t.stop()}}),t,null,[[0,8]])})))()},pauseFile:function(e){var t=this;return Object(c["a"])(regeneratorRuntime.mark((function n(){return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:return n.prev=0,n.next=3,f(e);case 3:return n.next=5,d();case 5:t.jobs=n.sent,n.next=11;break;case 8:n.prev=8,n.t0=n["catch"](0),alert("Failed to pause file!\n\n".concat(n.t0.message));case 11:case"end":return n.stop()}}),n,null,[[0,8]])})))()},resumeFile:function(e){var t=this;return Object(c["a"])(regeneratorRuntime.mark((function n(){return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:return n.prev=0,n.next=3,b(e);case 3:return n.next=5,d();case 5:t.jobs=n.sent,n.next=11;break;case 8:n.prev=8,n.t0=n["catch"](0),alert("Failed to resume file!\n\n".concat(n.t0.message));case 11:case"end":return n.stop()}}),n,null,[[0,8]])})))()},cancelFile:function(e){var t=this;return Object(c["a"])(regeneratorRuntime.mark((function n(){return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:return n.prev=0,n.next=3,v(e);case 3:return n.next=5,d();case 5:t.jobs=n.sent,n.next=11;break;case 8:n.prev=8,n.t0=n["catch"](0),alert("Failed to cancel file!\n\n".concat(n.t0.message));case 11:case"end":return n.stop()}}),n,null,[[0,8]])})))()},repeatFile:function(e){var t=this;return Object(c["a"])(regeneratorRuntime.mark((function n(){return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:return n.prev=0,n.next=3,g(e);case 3:return n.next=5,d();case 5:t.jobs=n.sent,n.next=11;break;case 8:n.prev=8,n.t0=n["catch"](0),alert("Failed to repeat file!\n\n".concat(n.t0.message));case 11:case"end":return n.stop()}}),n,null,[[0,8]])})))()},deleteFile:function(e){var t=this;return Object(c["a"])(regeneratorRuntime.mark((function n(){return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:return n.prev=0,n.next=3,h(e);case 3:return n.next=5,d();case 5:t.jobs=n.sent,n.next=11;break;case 8:n.prev=8,n.t0=n["catch"](0),alert("Failed to delete file!\n\n".concat(n.t0.message));case 11:case"end":return n.stop()}}),n,null,[[0,8]])})))()},suspendPrinter:function(e){var t=this;return Object(c["a"])(regeneratorRuntime.mark((function n(){return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:return n.prev=0,n.next=3,y(e);case 3:return n.next=5,w();case 5:t.printers=n.sent,t.printers.some((function(t){return t.Hostname===e&&null!==t.JobFile}))&&alert("This printer will be suspended as soon as the current print job has finished"),n.next=12;break;case 9:n.prev=9,n.t0=n["catch"](0),alert("Failed to suspend printer!\n\n".concat(n.t0.message));case 12:case"end":return n.stop()}}),n,null,[[0,9]])})))()},resumePrinter:function(e){var t=this;return Object(c["a"])(regeneratorRuntime.mark((function n(){return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:return n.prev=0,n.next=3,P(e);case 3:return n.next=5,w();case 5:t.printers=n.sent,n.next=11;break;case 8:n.prev=8,n.t0=n["catch"](0),alert("Failed to resume printer!\n\n".concat(n.t0.message));case 11:case"end":return n.stop()}}),n,null,[[0,8]])})))()},deletePrinter:function(e){var t=this;return Object(c["a"])(regeneratorRuntime.mark((function n(){return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:return n.prev=0,n.next=3,T(e);case 3:return n.next=5,w();case 5:t.printers=n.sent,n.next=11;break;case 8:n.prev=8,n.t0=n["catch"](0),alert("Failed to delete printer!\n\n".concat(n.t0.message));case 11:case"end":return n.stop()}}),n,null,[[0,8]])})))()}}},A=V,G=(n("b947"),n("b0a0"),Object(_["a"])(A,q,I,!1,null,"b76c3768",null)),Q=G.exports;r["default"].config.productionTip=!1,r["default"].use(a["a"]),r["default"].use(s["a"]),new r["default"]({el:"#app",render:function(e){return e(Q)}})},b0a0:function(e,t,n){"use strict";n("452c")},b947:function(e,t,n){"use strict";n("e223")},e223:function(e,t,n){}});
//# sourceMappingURL=app.90fdbcec.js.map