const API_URL = "http://localhost:5131/api";
const SESSION_KEY = "resenhario_usuario";

function usuarioAtual() {
    try {
        const usuario = JSON.parse(localStorage.getItem(SESSION_KEY) || "null");
        // Remove a sessão antiga, que guardava somente o ID e não possuía token assinado.
        if (usuario && !usuario.token) {
            localStorage.removeItem(SESSION_KEY);
            return null;
        }
        return usuario;
    } catch {
        localStorage.removeItem(SESSION_KEY);
        return null;
    }
}

async function api(caminho, opcoes = {}) {
    const usuario = usuarioAtual();
    const headers = { "Content-Type": "application/json", ...(opcoes.headers || {}) };
    if (usuario?.token) headers.Authorization = `Bearer ${usuario.token}`;
    const resposta = await fetch(`${API_URL}${caminho}`, { ...opcoes, headers });
    if (!resposta.ok) {
        const texto = await resposta.text();
        let mensagem = texto || `Erro HTTP ${resposta.status}`;
        try {
            const json = JSON.parse(texto);
            mensagem = json.mensagem || json.title || json.detail || json.message || texto;
        } catch { /* A resposta pode ser texto simples. */ }
        if (resposta.status === 401 && usuario?.token) {
            localStorage.removeItem(SESSION_KEY);
        }
        throw new Error(mensagem);
    }
    if (resposta.status === 204) return null;
    const texto = await resposta.text();
    return texto ? JSON.parse(texto) : null;
}

async function buscarCatalogo(tipo) { return api(`/${tipo}`); }

document.addEventListener("DOMContentLoaded", () => {
    carregarCatalogo();
    configurarBuscaLivros();
    configurarBuscaFilmes();
    configurarBuscaSeries();
    configurarLogin();
    configurarEstante();
    configurarReviews();
    configurarAdaptacoes();
    atualizarNavegacao();
});

function atualizarNavegacao() {
    const usuario = usuarioAtual();
    document.querySelectorAll('a[href*="login.html"]').forEach(a => {
        if (usuario) a.textContent = `Olá, ${usuario.nome}`;
    });
}

async function carregarCatalogo() {
    const caminho = location.pathname.toLowerCase();
    if (caminho.endsWith("/index.html") || caminho.endsWith("/")) {
        await Promise.allSettled([
            carregarLivrosEm("inicio-livros", "inicio-mensagem-livros"),
            carregarFilmesEm("inicio-filmes", "inicio-mensagem-filmes"),
            carregarSeriesEm("inicio-series", "inicio-mensagem-series")
        ]);
    }
    if (caminho.endsWith("/livros.html")) await carregarLivrosEm("lista-livros", "mensagem-catalogo", true);
    if (caminho.endsWith("/filmes.html")) await carregarFilmesEm("lista-filmes", "mensagem-filmes", true);
    if (caminho.endsWith("/series.html")) await carregarSeriesEm("lista-series", "mensagem-catalogo-series", true);
}

function criarCapaVazia(titulo) {
    const div = document.createElement("div");
    div.className = "capa-vazia";
    div.textContent = titulo || "Sem imagem";
    return div;
}

function criarCard(item) {
    const card = document.createElement("article");
    card.className = "card-capa";
    if (item.imagem) {
        const img = document.createElement("img");
        img.src = item.imagem;
        img.alt = `Capa de ${item.titulo || "obra"}`;
        img.loading = "lazy";
        img.onerror = () => img.replaceWith(criarCapaVazia(item.titulo));
        card.appendChild(img);
    } else card.appendChild(criarCapaVazia(item.titulo));

    const info = document.createElement("div");
    info.className = "card-info";
    const titulo = document.createElement("h3");
    titulo.textContent = item.titulo || "Sem título";
    const autor = document.createElement("p");
    autor.textContent = item.autor || "";
    const descricao = document.createElement("p");
    descricao.textContent = item.descricao || "";
    info.append(titulo, autor, descricao);
    if (item.botao) {
        const botao = document.createElement("button");
        botao.textContent = item.botao.texto;
        botao.addEventListener("click", item.botao.acao);
        info.appendChild(botao);
    }
    card.appendChild(info);
    return card;
}

function exigirLogin() {
    const usuario = usuarioAtual();
    if (!usuario?.token) {
        alert("Entre ou crie uma conta para usar sua estante e publicar resenhas.");
        location.href = "login.html";
        return null;
    }
    return usuario;
}

async function adicionarAEstante(ids) {
    if (!exigirLogin()) return;
    const status = ids.idLivros ? "Quero ler" : "Quero ver";
    try {
        await api("/Estante", {
            method: "POST",
            body: JSON.stringify({ idLivros: ids.idLivros || null, idFilme: ids.idFilme || null, idSerie: ids.idSerie || null, status })
        });
        alert("Adicionado à sua estante!");
    } catch (e) {
        alert(e.message.includes("já está") ? "Esse item já está na sua estante." : `Não foi possível salvar: ${e.message}`);
    }
}

async function carregarLivrosEm(listaId, mensagemId, permitirEstante = false) {
    const lista = document.getElementById(listaId), msg = document.getElementById(mensagemId);
    if (!lista || !msg) return;
    try {
        const livros = await buscarCatalogo("Livro");
        lista.replaceChildren();
        if (!livros.length) { msg.textContent = "Nenhum livro cadastrado ainda."; return; }
        msg.textContent = "";
        livros.forEach(l => lista.appendChild(criarCard({
            titulo: l.titulo, autor: l.autor, imagem: l.capaUrl, descricao: l.sinopse,
            botao: permitirEstante ? { texto: "＋ Minha estante", acao: () => adicionarAEstante({ idLivros: l.idLivros }) } : null
        })));
    } catch (e) { msg.textContent = "Não foi possível carregar os livros. Confira se a API está ligada."; console.error(e); }
}

async function carregarFilmesEm(listaId, mensagemId, permitirEstante = false) {
    const lista = document.getElementById(listaId), msg = document.getElementById(mensagemId);
    if (!lista || !msg) return;
    try {
        const filmes = await buscarCatalogo("Filme");
        lista.replaceChildren();
        if (!filmes.length) { msg.textContent = "Nenhum filme cadastrado ainda."; return; }
        msg.textContent = "";
        filmes.forEach(f => lista.appendChild(criarCard({
            titulo: f.titulo, autor: f.diretor, imagem: f.posterUrl,
            descricao: f.anoLancamento ? `Lançamento: ${f.anoLancamento}` : "",
            botao: permitirEstante ? { texto: "＋ Minha estante", acao: () => adicionarAEstante({ idFilme: f.idFilme }) } : null
        })));
    } catch (e) { msg.textContent = "Não foi possível carregar os filmes. Confira se a API está ligada."; console.error(e); }
}

async function carregarSeriesEm(listaId, mensagemId, permitirEstante = false) {
    const lista = document.getElementById(listaId), msg = document.getElementById(mensagemId);
    if (!lista || !msg) return;
    try {
        const series = await buscarCatalogo("Serie");
        lista.replaceChildren();
        if (!series.length) { msg.textContent = "Nenhuma série cadastrada ainda. Pesquise abaixo para adicionar."; return; }
        msg.textContent = "";
        series.forEach(s => lista.appendChild(criarCard({
            titulo: s.titulo, imagem: s.imagemUrl,
            descricao: s.dataEstreia ? `Estreia: ${s.dataEstreia}` : s.sinopse,
            botao: permitirEstante ? { texto: "＋ Minha estante", acao: () => adicionarAEstante({ idSerie: s.idSerie }) } : null
        })));
    } catch (e) { msg.textContent = "Não foi possível carregar séries. Confira se a tabela Series existe e se a API está ligada."; console.error(e); }
}

function configurarBuscaLivros() {
    const form = document.getElementById("form-busca-livros");
    if (!form) return;
    form.addEventListener("submit", async e => {
        e.preventDefault();
        const termo = document.getElementById("termo-livro").value.trim();
        const msg = document.getElementById("mensagem-busca"), area = document.getElementById("resultados-busca");
        if (!termo) return;
        area.replaceChildren(); msg.textContent = "Pesquisando livros no Google Books...";
        try {
            const livros = await api(`/Livro/buscar?q=${encodeURIComponent(termo)}`);
            if (!livros.length) { msg.textContent = "Nenhum livro encontrado."; return; }
            msg.textContent = `${livros.length} resultado(s). Clique para salvar no catálogo.`;
            livros.forEach(livro => {
                const card = criarCard({ titulo: livro.titulo, autor: livro.autor, imagem: livro.capaUrl,
                    descricao: livro.anoPublicacao ? `Publicado em ${livro.anoPublicacao}` : livro.sinopse || "" });
                const botao = document.createElement("button");
                botao.textContent = "Adicionar ao catálogo";
                botao.addEventListener("click", () => salvarLivro(livro, botao, msg));
                card.querySelector(".card-info").appendChild(botao);
                area.appendChild(card);
            });
        } catch (err) { console.error(err); msg.textContent = `Não foi possível pesquisar livros: ${err.message}`; }
    });
}

async function salvarLivro(livro, botao, msg) {
    botao.disabled = true; botao.textContent = "Salvando...";
    try {
        await api("/Livro", { method: "POST", body: JSON.stringify({ googleBId: livro.id, titulo: livro.titulo, autor: livro.autor || "Autor não informado", sinopse: livro.sinopse || null, isbn: livro.isbn || null, capaUrl: livro.capaUrl || null }) });
        botao.textContent = "Adicionado";
        msg.textContent = `“${livro.titulo}” foi adicionado ao catálogo.`;
        await Promise.allSettled([carregarLivrosEm("lista-livros", "mensagem-catalogo", true), carregarLivrosEm("inicio-livros", "inicio-mensagem-livros")]);
    } catch (e) {
        if (e.message.toLowerCase().includes("já está") || e.message.toLowerCase().includes("isbn") || e.message.toLowerCase().includes("catálogo")) {
            botao.textContent = "Já cadastrado";
            botao.disabled = true;
        } else { botao.disabled = false; botao.textContent = "Tentar novamente"; }
        msg.textContent = `Não foi possível salvar o livro: ${e.message}`;
    }
}

function configurarBuscaFilmes() {
    const form = document.getElementById("form-busca-filmes");
    if (!form) return;
    form.addEventListener("submit", async e => {
        e.preventDefault();
        const termo = document.getElementById("termo-filme").value.trim();
        const msg = document.getElementById("mensagem-busca-filmes"), area = document.getElementById("resultados-filmes");
        if (!termo) return;
        area.replaceChildren(); msg.textContent = "Pesquisando filmes no TMDB...";
        try {
            const filmes = await api(`/Filme/buscar?q=${encodeURIComponent(termo)}`);
            if (!filmes.length) { msg.textContent = "Nenhum filme encontrado."; return; }
            msg.textContent = `${filmes.length} filme(s) encontrado(s).`;
            filmes.forEach(f => {
                const card = criarCard({ titulo: f.titulo, autor: f.diretor || "Diretor não informado", imagem: f.posterUrl, descricao: f.anoLancamento ? `Lançamento: ${f.anoLancamento}` : "" });
                const botao = document.createElement("button");
                botao.textContent = "Adicionar ao catálogo";
                botao.addEventListener("click", () => salvarFilme(f, botao, msg));
                card.querySelector(".card-info").appendChild(botao);
                area.appendChild(card);
            });
        } catch (err) { console.error(err); msg.textContent = `Não foi possível pesquisar filmes: ${err.message}`; }
    });
}

async function salvarFilme(filme, botao, msg) {
    botao.disabled = true; botao.textContent = "Salvando...";
    try {
        await api("/Filme", { method: "POST", body: JSON.stringify({ tmdbId: filme.tmdbId, titulo: filme.titulo, diretor: filme.diretor || null, anoLancamento: filme.anoLancamento, posterUrl: filme.posterUrl || null }) });
        botao.textContent = "Adicionado";
        msg.textContent = `“${filme.titulo}” foi salvo no catálogo.`;
        await Promise.allSettled([carregarFilmesEm("lista-filmes", "mensagem-filmes", true), carregarFilmesEm("inicio-filmes", "inicio-mensagem-filmes")]);
        await carregarOpcoesAdaptacoes();
    } catch (e) {
        if (e.message.toLowerCase().includes("catálogo") || e.message.toLowerCase().includes("já")) { botao.textContent = "Já cadastrado"; botao.disabled = true; }
        else { botao.disabled = false; botao.textContent = "Tentar novamente"; }
        msg.textContent = `Não foi possível salvar: ${e.message}`;
    }
}

function configurarBuscaSeries() {
    const form = document.getElementById("form-busca-series");
    if (!form) return;
    form.addEventListener("submit", async e => {
        e.preventDefault();
        const termo = document.getElementById("termo-serie").value.trim();
        const msg = document.getElementById("mensagem-busca-series"), area = document.getElementById("resultados-series");
        area.replaceChildren(); msg.textContent = "Pesquisando séries...";
        try {
            const resultados = await api(`/Serie/buscar?q=${encodeURIComponent(termo)}`);
            if (!resultados.length) { msg.textContent = "Nenhuma série encontrada."; return; }
            msg.textContent = `${Math.min(resultados.length, 12)} série(s) encontrada(s).`;
            resultados.slice(0, 12).forEach(resultado => {
                const serie = {
                    fonteId: resultado.fonteId, titulo: resultado.titulo || "Série",
                    sinopse: resultado.sinopse || "",
                    imagemUrl: resultado.imagemUrl || "", dataEstreia: resultado.dataEstreia || ""
                };
                const card = criarCard({ titulo: serie.titulo, imagem: serie.imagemUrl, descricao: serie.dataEstreia ? `Estreia: ${serie.dataEstreia}` : serie.sinopse });
                const botao = document.createElement("button"); botao.textContent = "Adicionar ao catálogo";
                botao.addEventListener("click", async () => {
                    botao.disabled = true; botao.textContent = "Salvando...";
                    try {
                        await api("/Serie", { method: "POST", body: JSON.stringify(serie) });
                        botao.textContent = "Adicionado"; msg.textContent = `“${serie.titulo}” foi salva no catálogo.`;
                        await Promise.allSettled([carregarSeriesEm("lista-series", "mensagem-catalogo-series", true), carregarSeriesEm("inicio-series", "inicio-mensagem-series")]);
                    } catch (err) { botao.disabled = false; botao.textContent = "Tentar novamente"; msg.textContent = err.message; }
                });
                card.querySelector(".card-info").appendChild(botao); area.appendChild(card);
            });
        } catch (err) { console.error(err); msg.textContent = `Não foi possível pesquisar séries: ${err.message}`; }
    });
}

function configurarLogin() {
    const form = document.getElementById("form-login");
    if (!form) return;
    const alternar = document.getElementById("alternar-cadastro"), campos = document.getElementById("campos-cadastro");
    const msg = document.getElementById("mensagem-login"), sair = document.getElementById("sair-conta");
    let cadastro = false;
    const render = () => {
        if (campos) campos.hidden = !cadastro;
        form.querySelector('button[type="submit"]').textContent = cadastro ? "Criar conta" : "Entrar";
        if (alternar) alternar.textContent = cadastro ? "Já tenho uma conta" : "Criar conta";
        const usuario = usuarioAtual();
        if (sair) sair.hidden = !usuario;
        if (usuario) msg.textContent = `Você está conectado como ${usuario.nome}.`;
    };
    alternar?.addEventListener("click", () => { cadastro = !cadastro; render(); });
    sair?.addEventListener("click", () => {
        localStorage.removeItem(SESSION_KEY); render(); atualizarNavegacao();
        msg.textContent = "Você saiu da conta.";
    });
    render();
    form.addEventListener("submit", async e => {
        e.preventDefault(); msg.textContent = "Processando...";
        const email = document.getElementById("email").value.trim(), senha = document.getElementById("senha").value;
        try {
            const dados = cadastro
                ? { nome: document.getElementById("nome").value.trim(), email, senha }
                : { email, senha };
            const usuario = await api(cadastro ? "/Auth/cadastro" : "/Auth/login", { method: "POST", body: JSON.stringify(dados) });
            localStorage.setItem(SESSION_KEY, JSON.stringify(usuario));
            msg.textContent = cadastro ? "Conta criada com sucesso!" : "Login realizado!";
            render(); atualizarNavegacao();
            setTimeout(() => location.href = "../index.html", 500);
        } catch (err) { msg.textContent = err.message || "Não foi possível entrar."; }
    });
}

async function configurarEstante() {
    const area = document.getElementById("itens-estante"), msg = document.getElementById("mensagem-estante");
    if (!area || !msg) return;
    if (!usuarioAtual()?.token) { msg.textContent = "Entre na sua conta para visualizar sua estante."; return; }
    msg.textContent = "Carregando sua estante...";
    try {
        const itens = await api("/Estante"); area.replaceChildren();
        renderizarEstatisticasLeitura(itens);
        if (!itens.length) { msg.textContent = "Sua estante está vazia. Pesquise livros, filmes ou séries e adicione o que quiser guardar."; return; }
        msg.textContent = "";
        itens.forEach(item => {
            const card = criarCard({ titulo: item.titulo, autor: item.autor, descricao: item.tipo });
            const info = card.querySelector(".card-info"), select = document.createElement("select");
            const statuses = item.tipo === "Livro" ? ["Quero ler", "Lendo", "Lido", "Abandonado"] : ["Quero ver", "Assistindo", "Assistido"];
            statuses.forEach(status => {
                const option = document.createElement("option"); option.value = status; option.textContent = status; option.selected = status === item.status; select.appendChild(option);
            });
            select.setAttribute("aria-label", `Status de ${item.titulo}`);
            select.addEventListener("change", async () => {
                select.disabled = true;
                const statusAnterior = item.status;
                try {
                    await api(`/Estante/${item.idEstante}`, { method: "PUT", body: JSON.stringify({ status: select.value }) });
                    item.status = select.value;
                    renderizarEstatisticasLeitura(itens);
                } catch (err) {
                    select.value = statusAnterior;
                    msg.textContent = `Não foi possível atualizar: ${err.message}`;
                } finally { select.disabled = false; }
            });
            const remover = document.createElement("button"); remover.textContent = "Remover";
            remover.addEventListener("click", async () => {
                if (!confirm("Remover este item da sua estante?")) return;
                remover.disabled = true;
                try {
                    await api(`/Estante/${item.idEstante}`, { method: "DELETE" }); card.remove();
                    if (!area.children.length) msg.textContent = "Sua estante está vazia.";
                } catch (err) { remover.disabled = false; msg.textContent = `Não foi possível remover: ${err.message}`; }
            });
            info.append(select, remover); area.appendChild(card);
        });
    } catch (e) { msg.textContent = `Não foi possível carregar sua estante: ${e.message}`; }
}


function renderizarEstatisticasLeitura(itens) {
    const area = document.getElementById("estatisticas-leitura");
    if (!area) return;
    const livros = itens.filter(item => item.tipo === "Livro");
    const estatisticas = [
        ["Itens na estante", itens.length],
        ["Livros lidos", livros.filter(item => item.status === "Lido").length],
        ["Em leitura", livros.filter(item => item.status === "Lendo").length],
        ["Quero ler", livros.filter(item => item.status === "Quero ler").length],
        ["Abandonados", livros.filter(item => item.status === "Abandonado").length]
    ];
    area.replaceChildren();
    estatisticas.forEach(([rotulo, valor]) => {
        const card = document.createElement("article"); card.className = "estatistica-card";
        const numero = document.createElement("strong"); numero.textContent = String(valor);
        const texto = document.createElement("span"); texto.textContent = rotulo;
        card.append(numero, texto); area.appendChild(card);
    });
}

async function configurarReviews() {
    const form = document.getElementById("form-review"), lista = document.getElementById("lista-reviews"), msg = document.getElementById("mensagem-reviews");
    if (!lista) return;
    const render = async () => {
        try {
            const reviews = await api("/Review"); lista.replaceChildren();
            if (!reviews.length) { lista.textContent = "Ainda não há resenhas. Seja a primeira pessoa a compartilhar sua opinião."; return; }
            reviews.forEach(review => {
                const card = document.createElement("article"); card.className = "review-card";
                const titulo = document.createElement("h3"); titulo.textContent = review.titulo || "Obra sem título";
                const nota = document.createElement("p"); nota.textContent = `Qualidade do livro: ${review.notaLivro ?? "—"}/5 estrelas`;
                const comentario = document.createElement("p"); comentario.textContent = review.comentario || "";
                const autor = document.createElement("small");
                autor.textContent = `Por ${review.nomeUsuario || "Usuário"} · ${new Date(review.dataCriacao).toLocaleDateString("pt-BR")}`;
                card.append(titulo, nota);
                if (review.tituloFilme && review.notaFidelidade != null) {
                    const fidelidade = document.createElement("p"); fidelidade.textContent = `Fidelidade de “${review.tituloFilme}”: ${review.notaFidelidade}/5 estrelas`;
                    card.appendChild(fidelidade);
                }
                card.append(comentario, autor); lista.appendChild(card);
            });
        } catch (e) { lista.textContent = `Não foi possível carregar as resenhas: ${e.message}`; console.error(e); }
    };

    if (form) {
        const livroSelect = document.getElementById("review-livro");
        const adaptacaoSelect = document.getElementById("review-adaptacao");
        const notaFidelidade = document.getElementById("review-nota-fidelidade");
        try {
            const livros = await api("/Livro"); livroSelect.replaceChildren();
            const placeholder = new Option("Selecione um livro", ""); placeholder.disabled = true; placeholder.selected = true; livroSelect.appendChild(placeholder);
            livros.forEach(livro => livroSelect.appendChild(new Option(livro.titulo, livro.idLivros)));
            await carregarOpcoesAdaptacoes();
        } catch (e) { if (msg) msg.textContent = `Não foi possível carregar o formulário: ${e.message}`; }
        livroSelect?.addEventListener("change", () => atualizarAdaptacoesDoLivro());
        adaptacaoSelect?.addEventListener("change", () => { if (notaFidelidade) notaFidelidade.disabled = !adaptacaoSelect.value; });
        form.addEventListener("submit", async e => {
            e.preventDefault();
            if (!exigirLogin()) return;
            const idLivro = Number(livroSelect.value), idAdaptacao = Number(adaptacaoSelect?.value || 0);
            const notaLivro = Number(document.getElementById("review-nota").value);
            const comentario = document.getElementById("review-comentario").value.trim();
            const nota = idAdaptacao ? Number(notaFidelidade.value) : null;
            if (!idLivro) { msg.textContent = "Selecione um livro."; return; }
            if (idAdaptacao && (!nota || nota < 1 || nota > 5)) { msg.textContent = "Informe a fidelidade da adaptação de 1 a 5 estrelas."; return; }
            try {
                await api("/Review", { method: "POST", body: JSON.stringify({ idLivro, idAdaptacao: idAdaptacao || null, notaLivro, comentario, notaFidelidade: nota, assistiuFilme: !!idAdaptacao }) });
                form.reset(); if (notaFidelidade) notaFidelidade.disabled = true;
                if (msg) msg.textContent = "Resenha publicada!";
                await render(); await carregarRankingAdaptacoes();
            } catch (err) { if (msg) msg.textContent = `Não foi possível publicar: ${err.message}`; }
        });
    }
    await render();
}

let adaptacoesCache = [];
async function carregarOpcoesAdaptacoes() {
    try { adaptacoesCache = await api("/Adaptacao"); }
    catch (e) { adaptacoesCache = []; console.warn("Não foi possível carregar adaptações", e); }
    atualizarAdaptacoesDoLivro();
}

function atualizarAdaptacoesDoLivro() {
    const livroSelect = document.getElementById("review-livro"), adaptacaoSelect = document.getElementById("review-adaptacao");
    const notaFidelidade = document.getElementById("review-nota-fidelidade");
    if (!livroSelect || !adaptacaoSelect) return;
    const idLivro = Number(livroSelect.value || 0);
    adaptacaoSelect.replaceChildren(new Option("Não avaliar uma adaptação", ""));
    adaptacoesCache.filter(a => a.idLivro === idLivro).forEach(a => adaptacaoSelect.appendChild(new Option(`${a.tituloFilme} (adaptação do livro)`, a.idAdaptacoes)));
    if (notaFidelidade) { notaFidelidade.disabled = !adaptacaoSelect.value; if (!adaptacaoSelect.value) notaFidelidade.value = ""; }
}

async function configurarAdaptacoes() {
    const form = document.getElementById("form-vincular-adaptacao");
    const msg = document.getElementById("mensagem-adaptacoes"), lista = document.getElementById("lista-adaptacoes");
    if (!form || !msg || !lista) return;
    const livroSelect = document.getElementById("adaptacao-livro"), filmeSelect = document.getElementById("adaptacao-filme");
    const render = async () => {
        try {
            const [livros, filmes, adaptacoes] = await Promise.all([api("/Livro"), api("/Filme"), api("/Adaptacao")]);
            adaptacoesCache = adaptacoes;
            preencherSelect(livroSelect, livros, "idLivros", "titulo", "Selecione um livro");
            preencherSelect(filmeSelect, filmes, "idFilme", "titulo", "Selecione um filme");
            lista.replaceChildren();
            if (!adaptacoes.length) { lista.textContent = "Ainda não há adaptações vinculadas."; return; }
            adaptacoes.forEach(a => {
                const card = document.createElement("article"); card.className = "review-card";
                const p = document.createElement("p"); p.textContent = `${a.tituloLivro} → ${a.tituloFilme}`; card.appendChild(p); lista.appendChild(card);
            });
        } catch (e) { msg.textContent = `Não foi possível carregar os dados: ${e.message}`; }
    };
    form.addEventListener("submit", async e => {
        e.preventDefault(); msg.textContent = "Vinculando adaptação...";
        try {
            await api("/Adaptacao", { method: "POST", body: JSON.stringify({ idLivro: Number(livroSelect.value), idFilme: Number(filmeSelect.value) }) });
            msg.textContent = "Adaptação vinculada com sucesso."; await render(); await carregarOpcoesAdaptacoes();
        } catch (err) { msg.textContent = `Não foi possível vincular: ${err.message}`; }
    });
    await render();
}

function preencherSelect(select, itens, idCampo, textoCampo, placeholder) {
    if (!select) return;
    select.replaceChildren();
    const opcao = new Option(placeholder, ""); opcao.disabled = true; opcao.selected = true; select.appendChild(opcao);
    itens.forEach(item => select.appendChild(new Option(item[textoCampo], item[idCampo])));
}

async function carregarRankingAdaptacoes() {
    const lista = document.getElementById("ranking-adaptacoes"), msg = document.getElementById("mensagem-ranking");
    if (!lista) return;
    try {
        const ranking = await api("/Adaptacao/ranking"); lista.replaceChildren();
        if (!ranking.length) { if (msg) msg.textContent = "O ranking aparecerá quando houver avaliações de fidelidade."; return; }
        if (msg) msg.textContent = "Médias calculadas a partir das avaliações da comunidade.";
        ranking.forEach((item, index) => {
            const card = document.createElement("article"); card.className = "review-card";
            const titulo = document.createElement("h3"); titulo.textContent = `${index + 1}. ${item.tituloLivro} → ${item.tituloFilme}`;
            const media = document.createElement("p"); media.textContent = `Fidelidade média: ${Number(item.mediaFidelidade).toLocaleString("pt-BR", { maximumFractionDigits: 2 })}/5 · ${item.totalAvaliacoes} avaliação(ões)`;
            card.append(titulo, media); lista.appendChild(card);
        });
    } catch (e) { if (msg) msg.textContent = `Não foi possível carregar o ranking: ${e.message}`; }
}

document.addEventListener("DOMContentLoaded", carregarRankingAdaptacoes);


async function configurarFeedSocial() {
    const lista = document.getElementById("feed-resenhas");
    const msg = document.getElementById("mensagem-feed");
    const anterior = document.getElementById("feed-anterior");
    const proxima = document.getElementById("feed-proxima");
    const paginaAtual = document.getElementById("feed-pagina");
    if (!lista) return;
    let pagina = 1, totalPaginas = 1;
    async function carregar() {
        lista.replaceChildren();
        if (msg) msg.textContent = "Carregando resenhas da comunidade...";
        try {
            const dados = await api(`/Review/feed?pagina=${pagina}&tamanho=10`);
            totalPaginas = Math.max(1, dados.totalPaginas || 1);
            if (!dados.itens?.length) { if (msg) msg.textContent = "Ainda não há resenhas publicadas."; }
            else {
                if (msg) msg.textContent = `${dados.total} resenha(s) publicada(s).`;
                dados.itens.forEach(review => {
                    const card = document.createElement("article"); card.className = "review-card";
                    const titulo = document.createElement("h3"); titulo.textContent = review.titulo || "Obra sem título";
                    const nota = document.createElement("p"); nota.textContent = `Qualidade literária: ${review.notaLivro ?? "—"}/5 estrelas`;
                    const comentario = document.createElement("p"); comentario.textContent = review.comentario || "";
                    const autor = document.createElement("small"); autor.textContent = `Por ${review.nomeUsuario || "Usuário"} · ${new Date(review.dataCriacao).toLocaleDateString("pt-BR")}`;
                    card.append(titulo, nota);
                    if (review.tituloFilme && review.notaFidelidade != null) { const fidelidade = document.createElement("p"); fidelidade.textContent = `Fidelidade de “${review.tituloFilme}”: ${review.notaFidelidade}/5 estrelas`; card.appendChild(fidelidade); }
                    card.append(comentario, autor); lista.appendChild(card);
                });
            }
            if (paginaAtual) paginaAtual.textContent = `Página ${pagina} de ${totalPaginas}`;
            if (anterior) anterior.disabled = pagina <= 1;
            if (proxima) proxima.disabled = pagina >= totalPaginas;
        } catch (e) { if (msg) msg.textContent = `Não foi possível carregar o feed: ${e.message}`; }
    }
    anterior?.addEventListener("click", async () => { if (pagina > 1) { pagina--; await carregar(); } });
    proxima?.addEventListener("click", async () => { if (pagina < totalPaginas) { pagina++; await carregar(); } });
    await carregar();
}

async function configurarPaginaRanking() {
    const lista = document.getElementById("pagina-ranking-adaptacoes");
    const msg = document.getElementById("mensagem-pagina-ranking");
    if (!lista) return;
    try {
        const ranking = await api("/Adaptacao/ranking"); lista.replaceChildren();
        if (!ranking.length) { msg.textContent = "O ranking será exibido quando houver avaliações de fidelidade."; return; }
        msg.textContent = "Classificação calculada pelas avaliações da comunidade.";
        ranking.forEach((item, index) => {
            const card = document.createElement("article"); card.className = "review-card";
            const titulo = document.createElement("h3"); titulo.textContent = `${index + 1}. ${item.tituloLivro} → ${item.tituloFilme}`;
            const media = document.createElement("p"); media.textContent = `Fidelidade média: ${Number(item.mediaFidelidade).toLocaleString("pt-BR", { maximumFractionDigits: 2 })}/5 · ${item.totalAvaliacoes} avaliação(ões)`;
            card.append(titulo, media); lista.appendChild(card);
        });
    } catch (e) { msg.textContent = `Não foi possível carregar o ranking: ${e.message}`; }
}

document.addEventListener("DOMContentLoaded", () => { configurarFeedSocial(); configurarPaginaRanking(); });
