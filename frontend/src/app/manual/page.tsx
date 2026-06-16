import { BookOpen } from "lucide-react";
import { readFileSync } from "fs";
import { join } from "path";
import ReactMarkdown from "react-markdown";

function getManualContent(): string {
  try {
    return readFileSync(join(process.cwd(), "MANUAL-USUARIO.md"), "utf-8");
  } catch {
    return "# Manual no encontrado\n\nEl archivo `MANUAL-USUARIO.md` no está disponible.";
  }
}

export default function ManualPage() {
  const content = getManualContent();

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
        <BookOpen className="h-6 w-6 text-primary" /> Manual de Usuario
      </h1>
      <div className="prose prose-invert prose-sm max-w-none
        prose-headings:font-display prose-headings:text-foreground
        prose-p:text-foreground/90 prose-p:leading-relaxed
        prose-a:text-primary prose-a:no-underline hover:prose-a:underline
        prose-code:text-primary prose-code:bg-secondary prose-code:px-1.5 prose-code:py-0.5 prose-code:rounded
        prose-pre:bg-card prose-pre:border prose-pre:border-border
        prose-blockquote:border-primary/40 prose-blockquote:text-muted-foreground
        prose-strong:text-foreground
        prose-li:text-foreground/90
        prose-table:text-sm prose-thead:border-border prose-tbody:border-border prose-tr:border-border
      ">
        <ReactMarkdown>{content}</ReactMarkdown>
      </div>
    </div>
  );
}
