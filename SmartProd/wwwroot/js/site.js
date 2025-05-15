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