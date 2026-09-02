(() => {
    'use strict';
    const element=document.getElementById('caseGuestOnboarding')||document.getElementById('caseGuestUnavailable'),form=document.getElementById('caseGuestOnboardingForm');
    if(!element||!window.bootstrap) return;
    const modal=new bootstrap.Modal(element,{backdrop:'static',keyboard:false,focus:true}),error=document.getElementById('caseGuestOnboardingError'),submit=document.getElementById('caseGuestOnboardingSubmit');
    modal.show();
    if(!form) return;
    element.addEventListener('shown.bs.modal',()=>document.getElementById('caseGuestUsername')?.focus(),{once:true});
    form.addEventListener('submit',async event=>{
        event.preventDefault();
        if(!form.reportValidity()) return;
        error.classList.add('d-none'); submit.disabled=true;
        try {
            const response=await fetch('/api/case-tycoon/guest',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json','RequestVerificationToken':document.querySelector('input[name="__RequestVerificationToken"]')?.value||''},body:JSON.stringify({username:document.getElementById('caseGuestUsername').value.trim()})});
            const result=await response.json().catch(()=>({message:'The account could not be created.'}));
            if(!response.ok) throw new Error(result.message||'The account could not be created.');
            window.location.replace('/CaseOpening');
        } catch(exception) { error.textContent=exception.message||'The account could not be created.'; error.classList.remove('d-none'); submit.disabled=false; }
    });
})();
