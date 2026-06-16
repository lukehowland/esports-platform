import { type NextRequest, NextResponse } from "next/server";

export const dynamic = "force-dynamic";

const GATEWAY = process.env.GATEWAY_URL ?? "http://localhost:8080";

type Params = Promise<{ path: string[] }>;

async function proxy(req: NextRequest, { params }: { params: Params }) {
  const { path } = await params;
  const pathStr = path.join("/");
  const search = req.nextUrl.search;
  const target = `${GATEWAY}/api/${pathStr}${search}`;

  const reqHeaders = new Headers(req.headers);
  reqHeaders.delete("host");
  reqHeaders.delete("connection");

  const isBodyMethod = !["GET", "HEAD"].includes(req.method);

  const upstream = await fetch(target, {
    method: req.method,
    headers: reqHeaders,
    body: isBodyMethod ? req.body : undefined,
    // @ts-expect-error duplex required for streaming request body
    duplex: isBodyMethod ? "half" : undefined,
    cache: "no-store",
  });

  const resHeaders = new Headers(upstream.headers);
  resHeaders.delete("transfer-encoding");

  return new NextResponse(upstream.body, {
    status: upstream.status,
    headers: resHeaders,
  });
}

export const GET = proxy;
export const POST = proxy;
export const PUT = proxy;
export const PATCH = proxy;
export const DELETE = proxy;
