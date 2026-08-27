# SmartExpense frontend

React and TypeScript frontend for SmartExpense, built with Vite.

## Local development

1. Copy `.env.example` to `.env.local` if you need to override the API proxy.
2. Start the ASP.NET Core API on its HTTP development profile.
3. Run `npm install` and `npm run dev` from this directory.

Browser requests to `/api` are proxied to `VITE_API_PROXY_TARGET`, which defaults
to `http://localhost:5239`.
