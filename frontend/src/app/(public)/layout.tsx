import { Navbar } from "@/components/layout/navbar";

export default function PublicLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <Navbar />
      <main className="container mx-auto max-w-7xl px-4 py-6 min-h-[calc(100vh-3.5rem)]">
        {children}
      </main>
    </>
  );
}
