import Link from "next/link";
import { Trophy, Users, Swords, BarChart3, ArrowRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { HudPanel, HudEyebrow } from "@/components/hud-panel";
import { LiveMatchShowcase } from "@/components/live-match-showcase";

const DOMINIOS = [
  { href: "/equipos",   icon: Users,    label: "Equipos",   desc: "Rosters, jugadores y filtros por país.",          color: "violet" as const },
  { href: "/torneos",   icon: Trophy,   label: "Torneos",   desc: "Inscripciones, premios y búsqueda por código.",   color: "lime"   as const },
  { href: "/partidas",  icon: Swords,   label: "Partidas",  desc: "Resultados y historial de enfrentamientos.",      color: "gold"   as const },
  { href: "/rankings",  icon: BarChart3, label: "Rankings", desc: "Top equipos por torneos, victorias y jugadores.", color: "muted"  as const },
];

const COLOR_BORDER: Record<string, string> = {
  violet: "border-violet/30 hover:border-violet/60 bg-violet/5",
  lime:   "border-lime/30 hover:border-lime/60 bg-lime/5",
  gold:   "border-gold/30 hover:border-gold/60 bg-gold/5",
  muted:  "border-line hover:border-violet/30 bg-elevated/40",
};

const COLOR_ICON: Record<string, string> = {
  violet: "text-violet",
  lime:   "text-lime",
  gold:   "text-gold",
  muted:  "text-muted-foreground",
};

export default function HomePage() {
  return (
    <div className="space-y-12">

      <section>
        <LiveMatchShowcase />

        <p className="mx-auto mt-5 max-w-2xl text-center text-sm text-muted-foreground">
          Plataforma de torneos eSports — microservicios .NET 10 + Apache Cassandra + RabbitMQ.
          24 queries distribuidas en 4 servicios, unificadas por un API Gateway.
        </p>

        <div className="mt-6 flex flex-wrap justify-center gap-3">
          <Button asChild size="lg">
            <Link href="/login">Ingresar <ArrowRight className="h-4 w-4 ml-1" /></Link>
          </Button>
          <Button asChild size="lg" variant="outline">
            <Link href="/rankings">Ver rankings</Link>
          </Button>
        </div>
      </section>

      {/* Navegación por dominio */}
      <section>
        <HudEyebrow className="block mb-4">explorar plataforma</HudEyebrow>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          {DOMINIOS.map(({ href, icon: Icon, label, desc, color }) => (
            <Link key={href} href={href}>
              <div className={`hud-clip border transition-colors p-4 h-full group ${COLOR_BORDER[color]}`}>
                <Icon className={`w-5 h-5 mb-3 ${COLOR_ICON[color]}`} />
                <p className={`font-display font-semibold text-base mb-1 group-hover:${COLOR_ICON[color]} transition-colors`}>
                  {label}
                </p>
                <p className="text-xs text-muted-foreground leading-relaxed">{desc}</p>
              </div>
            </Link>
          ))}
        </div>
      </section>

      {/* Arquitectura */}
      <section>
        <HudEyebrow className="block mb-4">arquitectura distribuida</HudEyebrow>
        <HudPanel className="p-5">
          <div className="flex flex-col items-center gap-3 text-xs font-mono">
            <div className="hud-clip-sm border border-violet/30 bg-violet/10 text-violet px-6 py-2">
              Browser :3000
            </div>
            <div className="w-px h-5 bg-line" />
            <div className="hud-clip-sm border border-lime/40 bg-lime/10 text-lime px-6 py-2 font-bold">
              Gateway :8080
            </div>
            <div className="w-px h-5 bg-line" />
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 w-full max-w-xl">
              {[
                { n: "teams",       p: "5001", q: "Q1–Q6"          },
                { n: "tournaments", p: "5002", q: "Q8–Q15, Q20–21" },
                { n: "matches",     p: "5003", q: "Q16–Q19"        },
                { n: "ranking",     p: "5004", q: "Q7, Q22–24"     },
              ].map(({ n, p, q }) => (
                <div key={n} className="hud-clip-sm border border-line bg-elevated text-center py-3 px-2">
                  <p className="font-bold text-foreground">{n}</p>
                  <p className="text-muted-foreground">:{p}</p>
                  <p className="text-muted-foreground/70 mt-0.5">{q}</p>
                </div>
              ))}
            </div>
            <div className="w-px h-5 bg-line" />
            <div className="hud-clip-sm border border-line bg-secondary/50 px-6 py-2 text-muted-foreground">
              Apache Cassandra :9042 — 24 tablas Chebotko
            </div>
          </div>
        </HudPanel>
      </section>

    </div>
  );
}
