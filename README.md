# ReservationMVC

Application web ASP.NET Core MVC (.NET 9) de réservation d'offres, avec panier, paiement Stripe, factures PDF et espace d'administration.

## Configuration des clés Stripe

Les clés Stripe ne sont **pas** stockées dans le dépôt. Avant de lancer l'application, définissez-les avec les *user secrets* de .NET :

```bash
cd ReservationMVC_Tidiane_Yasmina
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
```

Ou avec des variables d'environnement : `Stripe__SecretKey` et `Stripe__PublishableKey`.

Les clés de test se trouvent dans le tableau de bord Stripe (Developers → API keys).
