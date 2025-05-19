function buscarCnpj() {
    const cnpj = document.getElementById("Cnpj").value;

    if (!cnpj || cnpj.length < 14) {
        alert("Digite um CNPJ válido.");
        return;
    }

    fetch(`/Empresa/BuscarCnpj?cnpj=${cnpj}`)
        .then(res => {
            if (!res.ok) throw new Error("CNPJ não encontrado.");
            return res.json();
        })
        .then(data => {
            document.getElementById("RazaoSocial").value = data.nome;
            document.getElementById("NomeFantasia").value = data.fantasia || data.nome;
            document.getElementById("Email").value = data.email;
            document.getElementById("Telefone").value = data.telefone;
        })
        .catch(err => alert("Erro ao buscar CNPJ: " + err.message));
}

function previewImage(event) {
    var reader = new FileReader();
    reader.onload = function () {
        var output = document.getElementById('imagePreview');
        output.src = reader.result;
        output.style.display = 'block';
    }
    reader.readAsDataURL(event.target.files[0]);
}

document.addEventListener("DOMContentLoaded", function () {
    const checkboxes = document.querySelectorAll(".delete-checkbox");
    const deleteBtn = document.getElementById("deleteSelectedBtn");

    function updateButtonState() {
        const anyChecked = Array.from(checkboxes).some(c => c.checked);
        deleteBtn.disabled = !anyChecked;
    }

    checkboxes.forEach(cb => {
        cb.addEventListener("change", updateButtonState);
    });

    updateButtonState(); // initial state
});

// Função global para o modal chamar
window.submitForm = function (formId) {
    const form = document.getElementById(formId);
    if (form) {
        form.submit();
    } else {
        console.warn("Formulário não encontrado:", formId);
    }
};