(function(e){function t(t){for(var r,o,i=t[0],u=t[1],c=t[2],d=0,p=[];d<i.length;d++)o=i[d],Object.prototype.hasOwnProperty.call(a,o)&&a[o]&&p.push(a[o][0]),a[o]=0;for(r in u)Object.prototype.hasOwnProperty.call(u,r)&&(e[r]=u[r]);l&&l(t);while(p.length)p.shift()();return s.push.apply(s,c||[]),n()}function n(){for(var e,t=0;t<s.length;t++){for(var n=s[t],r=!0,i=1;i<n.length;i++){var u=n[i];0!==a[u]&&(r=!1)}r&&(s.splice(t--,1),e=o(o.s=n[0]))}return e}var r={},a={app:0},s=[];function o(t){if(r[t])return r[t].exports;var n=r[t]={i:t,l:!1,exports:{}};return e[t].call(n.exports,n,n.exports,o),n.l=!0,n.exports}o.m=e,o.c=r,o.d=function(e,t,n){o.o(e,t)||Object.defineProperty(e,t,{enumerable:!0,get:n})},o.r=function(e){"undefined"!==typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(e,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(e,"__esModule",{value:!0})},o.t=function(e,t){if(1&t&&(e=o(e)),8&t)return e;if(4&t&&"object"===typeof e&&e&&e.__esModule)return e;var n=Object.create(null);if(o.r(n),Object.defineProperty(n,"default",{enumerable:!0,value:e}),2&t&&"string"!=typeof e)for(var r in e)o.d(n,r,function(t){return e[t]}.bind(null,r));return n},o.n=function(e){var t=e&&e.__esModule?function(){return e["default"]}:function(){return e};return o.d(t,"a",t),t},o.o=function(e,t){return Object.prototype.hasOwnProperty.call(e,t)},o.p="/";var i=window["webpackJsonp"]=window["webpackJsonp"]||[],u=i.push.bind(i);i.push=t,i=i.slice();for(var c=0;c<i.length;c++)t(i[c]);var l=u;s.push([0,"chunk-vendors"]),n()})({0:function(e,t,n){e.exports=n("56d7")},"452c":function(e,t,n){},"56d7":function(e,t,n){"use strict";n.r(t);n("e260"),n("e6cf"),n("cca6"),n("a79d");var r=n("2b0e"),a=n("5f5b"),s=n("b1e0"),o=(n("ab8b"),n("2dd8"),function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",[n("b-button",{directives:[{name:"b-modal",rawName:"v-b-modal.modal-add-printer",modifiers:{"modal-add-printer":!0}}],attrs:{disabled:e.disabled,size:"sm",variant:"success"}},[n("b-icon",{attrs:{icon:"plus"}}),e._v(" Add Printer ")],1),n("b-modal",{attrs:{id:"modal-add-printer",title:"Add Printer","ok-disabled":""==e.hostname},on:{ok:e.ok}},[n("p",[e._v("Please enter the hostname of the new printer:")]),n("b-form-input",{attrs:{placeholder:"Hostname or IP address",autofocus:""},on:{keyup:function(t){return!t.type.indexOf("key")&&e._k(t.keyCode,"enter",13,t.key,"Enter")?null:e.ok.apply(null,arguments)}},model:{value:e.hostname,callback:function(t){e.hostname=t},expression:"hostname"}})],1)],1)}),i=[],u=n("1da1"),c=(n("96cf"),n("99af"),n("d3b7"),4e3),l="".concat(location.protocol,"//").concat(location.host,"/");function d(e,t){var n=arguments.length>2&&void 0!==arguments[2]?arguments[2]:null,r=arguments.length>3&&void 0!==arguments[3]?arguments[3]:null,a=arguments.length>4&&void 0!==arguments[4]?arguments[4]:c,s=l+t;if(n){var o=!1;for(var i in n)s+=(o?"&":"?")+i+"="+encodeURIComponent(n[i]),o=!0}var u=new XMLHttpRequest;return u.open(e,s),u.responseType="text",u.setRequestHeader("Content-Type","application/json"),u.timeout=a,new Promise((function(e,t){u.onload=function(){if(u.status>=200&&u.status<300)try{u.responseText?e(JSON.parse(u.responseText)):e(null)}catch(n){t(n)}else 0!==u.status?t(new Error("Server returned HTTP code ".concat(u.status," ").concat(u.statusText))):t(new Error("HTTP request failed"))},u.onabort=function(){t(new Error("Request aborted"))},u.onerror=function(){t(new Error("HTTP request failed"))},u.ontimeout=function(){t(new Error("HTTP request timed out"))},u.send(r)}))}function p(){return d("GET","printFarm/queue")}function f(){return d("GET","printFarm/printers")}function m(e){return d("PUT","printFarm/printer",{hostname:e})}function b(e){return d("DELETE","printFarm/printer",{hostname:e})}function h(e,t){var n=t instanceof Blob?t:new Blob([t]);return d("PUT","printFarm/job",{filename:e},n,0)}function g(e){return d("DELETE","printFarm/job",{index:e})}var v={props:{disabled:{default:!1,type:Boolean}},data:function(){return{hostname:""}},methods:{ok:function(){var e=this;return Object(u["a"])(regeneratorRuntime.mark((function t(){return regeneratorRuntime.wrap((function(t){while(1)switch(t.prev=t.next){case 0:return e.$bvModal.hide("modal-add-printer"),t.prev=1,t.next=4,m(e.hostname);case 4:e.hostname="",t.next=10;break;case 7:t.prev=7,t.t0=t["catch"](1),alert("Failed to add printer:\n\n".concat(t.t0.message));case 10:case"end":return t.stop()}}),t,null,[[1,7]])})))()}}},y=v,w=n("2877"),k=Object(w["a"])(y,o,i,!1,null,null,null),x=k.exports,T=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("b-badge",{attrs:{variant:e.statusVariant}},[e._v(" "+e._s(e.statusText)+" ")])},_=[],j={props:{status:{required:!0,type:String}},computed:{statusText:function(){var e=this.status;return this.status?"processing"===this.status&&(e="printing"):e="unknown",e[0].toUpperCase()+e.substring(1)},statusVariant:function(){switch(this.status){case"disconnected":return"danger";case"starting":return"info";case"updating":return"primary";case"off":return"danger";case"halted":return"danger";case"pausing":return"warning";case"paused":return"warning";case"resuming":return"secondary";case"processing":return"success";case"simulating":return"success";case"busy":return"warning";case"changingTool":return"primary";case"idle":return"info";default:return"dark"}}}},P=j,S=Object(w["a"])(P,T,_,!1,null,null,null),C=S.exports,F=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("b-button",{attrs:{disabled:e.disabled,loading:e.isBusy,size:"sm",variant:"primary"},on:{click:e.chooseFile}},[n("b-icon",{attrs:{icon:"cloud-upload"}}),e._v(" Upload File "),n("input",{ref:"fileInput",attrs:{type:"file",accept:".g,.gcode,.gc,.gco,.nc,.ngc,.tap",hidden:""},on:{change:e.fileSelected}})],1)},O=[],H=(n("b0c0"),{props:{disabled:{default:!1,type:Boolean}},data:function(){return{isBusy:!1}},methods:{chooseFile:function(){this.isBusy||this.$refs.fileInput.click()},fileSelected:function(e){return Object(u["a"])(regeneratorRuntime.mark((function t(){return regeneratorRuntime.wrap((function(t){while(1)switch(t.prev=t.next){case 0:if(!(e.target.files.length>0)){t.next=11;break}return t.prev=1,console.log(e.target.files[0]),t.next=5,h(e.target.files[0].name,e.target.files[0]);case 5:t.next=10;break;case 7:t.prev=7,t.t0=t["catch"](1),alert("Upload failed!\n\n"+t.t0.message);case 10:e.target.value="";case 11:case"end":return t.stop()}}),t,null,[[1,7]])})))()}}}),E=H,M=Object(w["a"])(E,F,O,!1,null,null,null),R=M.exports;r["default"].component("add-printer-button",x),r["default"].component("status-label",C),r["default"].component("upload-button",R);var J=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",{attrs:{id:"app"}},[n("b-container",{staticClass:"mt-3"},[n("h1",{staticClass:"mb-3 text-center"},[e._v(" Duet3D Print Farm Overview ")]),n("b-alert",{attrs:{show:!!e.errorMessage,variant:"warning"}},[n("b-icon",{staticClass:"mr-1",attrs:{icon:"exclamation-triangle"}}),e._v(" "+e._s(e.errorMessage)+" ")],1),n("b-card",{attrs:{"no-body":""},scopedSlots:e._u([{key:"header",fn:function(){return[n("span",[n("b-icon",{attrs:{icon:"card-list"}}),e._v(" Job Queue ")],1),n("upload-button",{attrs:{disabled:!!e.errorMessage}})]},proxy:!0}])},[n("b-alert",{staticClass:"mb-0",attrs:{show:0===e.jobs.length,variant:"info"}},[n("b-icon",{staticClass:"mr-1",attrs:{icon:"info-circle"}}),e._v(" No Jobs Available ")],1),n("b-table",{directives:[{name:"show",rawName:"v-show",value:e.jobs.length>0,expression:"jobs.length > 0"}],staticClass:"mb-0 job-table",attrs:{striped:"",hover:"",fields:e.jobFields,items:e.jobs},scopedSlots:e._u([{key:"cell(Filename)",fn:function(t){var r=t.item;return[n("b-icon",{attrs:{icon:e.getJobIcon(r),"icon-props":{fontScale:2}}}),e._v(" "+e._s(r.Filename)+" ")]}},{key:"cell(Progress)",fn:function(t){var r=t.item;return[null!==r.Progress||r.TimeCompleted?n("b-progress",{attrs:{max:1,"show-progress":"",animated:!r.TimeCompleted,variant:e.getJobProgressVariant(r)}},[n("b-progress-bar",{attrs:{value:r.TimeCompleted?1:r.Progress,label:(100*(r.TimeCompleted?1:r.Progress)).toFixed(1)+" %"}})],1):n("span",[e._v(" n/a ")])]}},{key:"cell(Delete)",fn:function(t){var r=t.item,a=t.index;return[n("b-button",{directives:[{name:"show",rawName:"v-show",value:!r.Hostname||!!r.TimeCompleted,expression:"!item.Hostname || !!item.TimeCompleted"}],attrs:{size:"sm",variant:"danger"},on:{click:function(t){return e.deleteFile(a)}}},[n("b-icon",{attrs:{icon:"trash"}})],1)]}}])})],1),n("b-card",{staticClass:"mt-3",attrs:{"no-body":""},scopedSlots:e._u([{key:"header",fn:function(){return[n("span",[n("b-icon",{attrs:{icon:"printer"}}),e._v(" Printer Management ")],1),n("add-printer-button",{attrs:{disabled:!!e.errorMessage}})]},proxy:!0}])},[n("b-alert",{staticClass:"mb-0",attrs:{show:0===e.printers.length,variant:"warning"}},[n("b-icon",{staticClass:"mr-1",attrs:{icon:"exclamation-triangle"}}),e._v(" No Jobs Available ")],1),n("b-table",{directives:[{name:"show",rawName:"v-show",value:e.printers.length>0,expression:"printers.length > 0"}],staticClass:"mb-0 printer-table",attrs:{striped:"",hover:"",fields:e.printerFields,items:e.printers},scopedSlots:e._u([{key:"cell(Hostname)",fn:function(t){var r=t.item;return[n("b-icon",{attrs:{icon:e.getPrinterIcon(r)}}),e._v(" "+e._s(r.Hostname)+" "),n("status-label",{staticClass:"ml-1",attrs:{status:r.Status}})]}},{key:"cell(Delete)",fn:function(t){return[n("b-button",{attrs:{size:"sm",variant:"danger"},on:{click:function(n){return e.deletePrinter(t.item.Hostname)}}},[n("b-icon",{attrs:{icon:"trash"}})],1)]}}])})],1)],1)],1)},D=[];n("25f0"),n("b680"),n("a15b");function N(e){var t=arguments.length>1&&void 0!==arguments[1]&&arguments[1];if(null===e||isNaN(e))return"n/a";e=Math.round(e),e<0&&(e=0);var n,r=[];return e>=3600&&(n=Math.floor(e/3600),n>0&&(r.push(n+"h"),e%=3600)),e>=60&&(n=Math.floor(e/60),n>0&&(r.push((e>9||!t?n:"0"+n)+"m"),e%=60)),e=e.toFixed(0),r.push((e>9||!t?e:"0"+e)+"s"),r.join(" ")}var L={data:function(){return{errorMessage:null,jobFields:[{key:"Filename",sortable:!0},{key:"Hostname",sortable:!0},{key:"TimeCreated",formatter:function(e){return e?new Date(e).toLocaleString():"n/a"},sortable:!0},{key:"Progress",sortable:!0},{key:"TimeLeft",formatter:function(e){return N(e)},sortable:!0},{key:"TimeCompleted",formatter:function(e){return e?new Date(e).toLocaleString():"n/a"},sortable:!0},{key:"Delete",label:"",sortable:!1}],jobs:[],printerFields:[{key:"Hostname",sortable:!0},{key:"Online",formatter:function(e){return e?"Yes":"No"},sortable:!0},{key:"JobFile",formatter:function(e){return e||"none"},sortable:!0},{key:"Delete",label:"",sortable:!1}],printers:[]}},mounted:function(){this.updateLoop()},methods:{getJobIcon:function(e){return e.TimeCompleted?"check":e.Hostname?this.printers.some((function(t){return t.Hostname===e.Hostname&&"pausing"===t.Status||"paused"===t.Status||"resuming"===t.Status||"cancelling"===t.Status}))?"pause":"play-fill":"asterisk"},getJobProgressVariant:function(e){return e.TimeCompleted?"success":e.Hostname&&this.printers.some((function(t){return t.Hostname===e.Hostname&&"pausing"===t.Status||"paused"===t.Status||"resuming"===t.Status||"cancelling"===t.Status}))?"warning":"primary"},getPrinterIcon:function(e){return e.Online?"check":"x"},updateLoop:function(){var e=this;return Object(u["a"])(regeneratorRuntime.mark((function t(){return regeneratorRuntime.wrap((function(t){while(1)switch(t.prev=t.next){case 0:return t.prev=0,t.next=3,p();case 3:return e.jobs=t.sent,t.next=6,f();case 6:e.printers=t.sent,e.errorMessage=null,t.next=14;break;case 10:t.prev=10,t.t0=t["catch"](0),e.jobs=e.printers=[],e.errorMessage=t.t0.toString();case 14:setTimeout(e.updateLoop,1e3);case 15:case"end":return t.stop()}}),t,null,[[0,10]])})))()},deleteFile:function(e){var t=this;return Object(u["a"])(regeneratorRuntime.mark((function n(){return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:return n.prev=0,n.next=3,g(e);case 3:return n.next=5,p();case 5:t.jobs=n.sent,n.next=11;break;case 8:n.prev=8,n.t0=n["catch"](0),alert("Failed to delete file!\n\n".concat(n.t0.message));case 11:case"end":return n.stop()}}),n,null,[[0,8]])})))()},deletePrinter:function(e){var t=this;return Object(u["a"])(regeneratorRuntime.mark((function n(){return regeneratorRuntime.wrap((function(n){while(1)switch(n.prev=n.next){case 0:return n.prev=0,n.next=3,b(e);case 3:return n.next=5,f();case 5:t.printers=n.sent,n.next=11;break;case 8:n.prev=8,n.t0=n["catch"](0),alert("Failed to delete printer!\n\n".concat(n.t0.message));case 11:case"end":return n.stop()}}),n,null,[[0,8]])})))()}}},q=L,I=(n("e55e"),n("b0a0"),Object(w["a"])(q,J,D,!1,null,"7a22d4fc",null)),B=I.exports;r["default"].config.productionTip=!1,r["default"].use(a["a"]),r["default"].use(s["a"]),new r["default"]({el:"#app",render:function(e){return e(B)}})},b0a0:function(e,t,n){"use strict";n("452c")},e55e:function(e,t,n){"use strict";n("e83a")},e83a:function(e,t,n){}});
//# sourceMappingURL=app.f88ac911.js.map