// Aplica máscara de CNPJ: 12.345.678/0001-90
function aplicarMascaraCnpj(valor) {
    return valor
        .replace(/\D/g, '') // remove tudo que não for número
        .replace(/^(\d{2})(\d)/, "$1.$2")
        .replace(/^(\d{2})\.(\d{3})(\d)/, "$1.$2.$3")
        .replace(/\.(\d{3})(\d)/, ".$1/$2")
        .replace(/(\d{4})(\d)/, "$1-$2")
        .substring(0, 18);
}

// Só permite números no campo e aplica máscara
function apenasNumeros(e) {
    let input = e.target;
    input.value = aplicarMascaraCnpj(input.value);
}

// Debounce para evitar requisições em excesso
let debounceTimeout;
function buscarCnpjAuto() {
    clearTimeout(debounceTimeout);
    debounceTimeout = setTimeout(() => {
        const input = document.getElementById("Cnpj");
        const cnpj = input.value.replace(/\D/g, '');

        if (cnpj.length === 14) {
            fetch(`/Empresa/BuscarCnpj?cnpj=${cnpj}`)
                .then(res => {
                    if (!res.ok) throw new Error("CNPJ não encontrado.");
                    return res.json();
                })
                .then(data => {
                    document.getElementById("RazaoSocial").value = data.nome || "";
                    document.getElementById("NomeFantasia").value = data.fantasia || data.nome || "";
                    document.getElementById("Email").value = data.email || "";
                    document.getElementById("Telefone").value = data.telefone || "";
                })
                .catch(err => {
                    alert("Erro ao buscar CNPJ: " + err.message);
                });
        }
    }, 600); // 600ms de atraso após digitação parar
}
// previa da imagem
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
        const anyChecked = Array.from(checkboxes).some(c => c.checked && !c.disabled);
        deleteBtn.disabled = !anyChecked;
    }

    checkboxes.forEach(cb => {
        cb.addEventListener("change", updateButtonState);
    });

    updateButtonState();

    const confirmDeleteBtn = document.getElementById("confirmDeleteBtn");

    confirmDeleteBtn.addEventListener("click", function () {
        const selectedIds = Array.from(checkboxes)
            .filter(cb => cb.checked && !cb.disabled)
            .map(cb => cb.closest(".card"))
            .filter(card => card && Number(card.getAttribute("data-estoque-atual")) === 0)
            .map(card => card.getAttribute("data-produto-id"));

        if (selectedIds.length === 0) {
            alert("Só é possível excluir produtos com estoque igual a 0.");
            return;
        }

        fetch('/Produtos/DeleteMultiple', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ ids: selectedIds })
        })
            .then(response => response.json())
            .then(result => {
                if (result.success) {
                    selectedIds.forEach(id => {
                        const card = document.querySelector('.card[data-produto-id="' + id + '"]');
                        if (card) card.closest('.col').remove();

                        const row = document.querySelector('tr[data-produto-id="' + id + '"]');
                        if (row) row.remove();
                    });

                    var modal = bootstrap.Modal.getInstance(document.getElementById('staticBackdrop'));
                    modal.hide();
                } else {
                    alert(result.message || "Erro ao excluir.");
                }
            })
            .catch(err => {
                alert("Erro ao excluir.");
                console.error(err);
            });
    });
});

// Função global para o modal chamar
function submitForm(formId) {
    const form = document.getElementById(formId);
    if (form) {
        form.submit();
    } else {
        console.error("Formulário não encontrado: " + formId);
    }
};



   
