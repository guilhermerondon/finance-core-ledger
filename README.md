<img width="100%" src="https://capsule-render.vercel.app/api?type=waving&color=8b5cf6&height=110&section=header&animation=fadeIn"/>

![.NET Core CI](https://github.com/guilhermerondon/finance-core-ledger/actions/workflows/ci-dotnet.yml/badge.svg)

# Finance Core Ledger (.NET 8)

Web API corporativa de alto desempenho desenvolvida em C#, projetada para servir como o motor transacional central do ecossistema. O sistema é responsável pela orquestração rigorosa de fluxos de caixa, conciliação e processamento de registros financeiros sob padrões estritos de integridade de dados e arquitetura desacoplada.

---

## 🚀 Tecnologias e Arquitetura

* **.NET 8 (C#)**: Utilização da plataforma moderna da Microsoft para construção de Web APIs assíncronas de altíssima performance, aproveitando as otimizações de compilação JIT (*Just-In-Time*) e gerenciamento de memória eficiente.
* **Entity Framework Core**: ORM (*Object-Relational Mapper*) robusto e otimizado para persistência relacional estável, controle transacional complexo, tratamento de concorrência e execução de migrações estruturadas (*Migrations*).
* **Identity & JWT (JSON Web Tokens)**: Infraestrutura de segurança avançada com criptografia simétrica para emissão, validação e gerenciamento de tokens de autenticação, implementando controle rígido de autorização baseado em perfis nos endpoints.
* **Repository Pattern**: Padrão de projeto estrutural implementado para criar uma abstração limpa entre a camada de acesso a dados (*Data Access Layer*) e as regras de negócio expostas nas controllers HTTP, elevando os níveis de testabilidade e desacoplamento.

---

## ⚙️ Engenharia e Segurança Transacional

O coração do ledger financeiro foi arquitetado sobre os princípios do SOLID e Clean Code, garantindo resiliência matemática no tratamento de saldos e transações. O pipeline HTTP possui:
1. Filtros globais de tratamento de exceções (*Global Exception Handling*) para mitigar vazamentos de logs de infraestrutura para o cliente.
2. Camada de validação prévia de requisições baseada em DTOs (*Data Transfer Objects*).
3. Proteção criptográfica nativa para garantir que nenhuma movimentação de ledger ocorra sem um payload autenticado e assinado digitalmente via JWT.

---

## 🛠️ Execução Local

### Pré-requisitos
* .NET 8 SDK instalado.
* Instância de banco de dados configurada (ou mapeada para o provedor definido no ambiente).

### Instalação e Inicialização
```bash
# Restaurar todas as dependências e pacotes NuGet do projeto
dotnet restore

# Executar a aplicação e subir o servidor Kestrel nativo
dotnet run
