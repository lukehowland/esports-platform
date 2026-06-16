import Link from "next/link";
import { Trophy, Users, Swords, BarChart3, Gamepad2, Network, ArrowRight } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

const dominios = [
  { href: "/equipos", icon: Users, title: "Equipos & Jugadores", desc: "Registrá tu equipo, armá el roster y explorá los planteles de la competencia.", color: "text-primary" },
  { href: "/torneos", icon: Trophy, title: "Torneos", desc: "Creá torneos, inscribí equipos, asigná premios y seguí el bracket.", color: "text-warning" },
  { href: "/partidas", icon: Swords, title: "Partidas", desc: "Registrá resultados y consultá el historial de enfrentamientos cara a cara.", color: "text-success" },
  { href: "/rankings", icon: BarChart3, title: "Rankings", desc: "Top-N de equipos por torneos y victorias, y los jugadores más activos.", color: "text-gold" },
];

const servicios = [
  { name: "teams", port: "5001", queries: "Q1–Q6", color: "bg-primary/10 border-primary/20" },
  { name: "tournaments", port: "5002", queries: "Q8–Q15, Q20, Q21", color: "bg-warning/10 border-warning/20" },
  { name: "matches", port: "5003", queries: "Q16–Q19", color: "bg-success/10 border-success/20" },
  { name: "ranking", port: "5004", queries: "Q7, Q22–Q24", color: "bg-gold/10 border-gold/20" },
];

export default function HomePage() {
  return (
    <div className="space-y-16">
      {/* Hero */}
      <section className="pt-8 pb-4 text-center">
        <div className="inline-flex items-center gap-2 rounded-full border border-primary/20 bg-primary/5 px-4 py-1.5 text-xs text-primary mb-6">
          <Gamepad2 className="h-3.5 w-3.5" />
          Sistemas Distribuidos — UNIVALLE
        </div>
        <h1 className="text-4xl sm:text-5xl font-bold tracking-tight text-foreground mb-4">
          Plataforma de{" "}
          <span className="text-primary">eSports</span>
        </h1>
        <p className="text-lg text-muted-foreground max-w-2xl mx-auto mb-8">
          Backend de microservicios .NET 10 + Apache Cassandra + RabbitMQ, con 24 queries
          distribuidas en 4 servicios independientes, todo unificado por un API Gateway.
        </p>
        <div className="flex flex-wrap gap-3 justify-center">
          <Button size="lg" asChild>
            <Link href="/login">
              Ingresar como organizador o capitán
            </Link>
          </Button>
          <Button size="lg" variant="outline" asChild>
            <Link href="/rankings">
              Ver rankings <ArrowRight className="h-4 w-4 ml-1" />
            </Link>
          </Button>
        </div>
      </section>

      {/* Dominios */}
      <section>
        <h2 className="text-xl font-semibold text-foreground mb-6">Explora la plataforma</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {dominios.map(({ href, icon: Icon, title, desc, color }) => (
            <Link key={href} href={href}>
              <Card className="h-full hover:border-primary/40 transition-colors group cursor-pointer">
                <CardHeader className="pb-2">
                  <Icon className={`h-8 w-8 mb-2 ${color}`} />
                  <CardTitle className="text-base group-hover:text-primary transition-colors">{title}</CardTitle>
                </CardHeader>
                <CardContent>
                  <CardDescription>{desc}</CardDescription>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      </section>

      {/* Arquitectura */}
      <section>
        <h2 className="text-xl font-semibold text-foreground mb-2">Arquitectura distribuida</h2>
        <p className="text-sm text-muted-foreground mb-6">
          El frontend habla con un solo endpoint (<code className="text-primary">:8080</code>) y el
          API Gateway YARP enruta cada request al microservicio dueño de la tabla.
        </p>
        <div className="rounded-xl border border-border bg-card p-6">
          <div className="flex flex-col items-center gap-3">
            {/* Browser → Gateway */}
            <div className="flex items-center gap-3 text-sm">
              <div className="rounded-lg border border-border bg-secondary px-4 py-2 text-foreground font-mono text-xs">
                Browser :3000
              </div>
              <Network className="h-4 w-4 text-primary" />
              <div className="rounded-lg border border-primary/40 bg-primary/10 px-4 py-2 text-primary font-mono text-xs font-bold">
                Gateway :8080
              </div>
            </div>
            {/* Gateway → Servicios */}
            <div className="w-px h-4 bg-border" />
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 w-full">
              {servicios.map(({ name, port, queries, color }) => (
                <div key={name} className={`rounded-lg border ${color} p-3 text-center`}>
                  <div className="font-mono text-xs font-bold text-foreground">{name}</div>
                  <div className="text-xs text-muted-foreground mt-0.5">:{port}</div>
                  <div className="text-xs text-muted-foreground mt-1 opacity-70">{queries}</div>
                </div>
              ))}
            </div>
            {/* Cassandra */}
            <div className="w-px h-4 bg-border" />
            <div className="rounded-lg border border-border bg-secondary/50 px-6 py-2 text-xs text-muted-foreground font-mono">
              Apache Cassandra :9042 — 24 tablas Chebotko
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
