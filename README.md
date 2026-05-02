# Finance Core Ledger

API robusta construída em **C#** para gerenciamento de fluxo de caixa e transações, servindo como motor financeiro do ecossistema.

## 🚀 Tecnologias e Arquitetura

- **.NET 8 (C#)**: API RESTful de alta performance.
- **Entity Framework Core**: ORM configurado com SQLite para persistência rápida.
- **Identity & JWT**: Sistema de autenticação e autorização seguro para controle de acesso.
- **Repository Pattern**: Abstração limpa entre a camada de dados e os controllers.

## 🧠 Filosofia

A disciplina nos números reflete a disciplina na vida. Assim como mantenho o rigor em treinar 6 vezes na semana, garanto a integridade transacional nesta API: sem falhas, sem inconsistências, foco puro no resultado e arquitetura limpa.

## 🛠️ Como Executar

```bash
dotnet restore
dotnet run
```
A API estará disponível para receber conexões do frontend, exposta via Swagger para testes manuais.
