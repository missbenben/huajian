
/* 错误提示效验 */
var regs = {
    phone: /^1[0-9]{10}$/,
    password: function (val) {
        /* var modes = 0;
         var flag = false;
         if (val.length >= 6 && val.length <= 20) return true;
         /!* if (/\d/.test(val)) modes++; //数字
         if (/[a-z]/.test(val)) modes++; //小写
         if (/[A-Z]/.test(val)) modes++; //大写
         if (/\W/.test(val)) modes++; //特殊字符
         if (modes > 2) flag = true;
         return flag;*!/*/
        if(/^[a-zA-Z0-9]{6,20}$/g.test(val)) {
            if(/^\d+$/g.test(val) || /^[a-zA-Z]+$/g.test(val)) {
                return false;
            } else {
                return true;
            }
        } else {
            return false;
        }
    },
    email: /^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$/,
    // telephone:/(\d{3,4}-)?\d{6,8}/,
    telephone :/^[0-9]|-/,
   // name : /^[\u4E00-\u9FA5]{1,5}$/,
   //name : /^[\u4E00-\u9FA5]|\(|\){1,5}$/,
    name: function(val) {

        if(/^[\u4E00-\u9FA5]{1,5}$/g.test(val)) {
            return true;
        } else {
            if(/\(|\)/g.test(val)) {
                var test = /[^\(\)]+(?=\))/g.exec(val);
                if(!test) {
                    return false;
                } else {
                    var str = val.split("(")[0]+val.split(")")[1];
                    if(/^[\u4E00-\u9FA5]{1,5}$/g.test(str)) {
                        return true;
                    } else {
                        return false;
                    }
                }
            } else {
                return false;
            }
        }

    },
    account :/^([a-z]|[A-Z]|[0-9]){6,15}$/,
    organizationCode: /^([a-z]|[A-Z]|[0-9]){9}$/,
    enterprisePostcode: /^[0-9]{6}$/,
    licenseNumber: /^[0-9]{15}$/,
    enterpriseName: function (val) {
        if(val.length >=1 && val.length <=25) {
            return true;
        } else {
            return false;
        }
    },
    address: function (val) {
        if(val.length > 50) {
            return false;
        } else {
            return true;
        }
    }
};

//错误提示封装
function errPromotShow(dom,msg) {
    dom.siblings(".error-reminder").html(msg).show();
    dom.parents(".form-group").addClass("error");
}

function removeErr (dom) {
    dom.siblings(".error-reminder").hide();
    dom.parents(".form-group").removeClass("error");
}

