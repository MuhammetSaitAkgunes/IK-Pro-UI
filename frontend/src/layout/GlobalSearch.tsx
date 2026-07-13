import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { appRoutes, navIcons, type Role } from "../routes";

type Item = { label: string; hint: string; icon: string; path: string };

export function GlobalSearch() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);

  const items: Item[] = !query.trim() || query.trim().length < 2
    ? []
    : appRoutes
        .filter((r) => r.navKey === r.key && r.roles.includes((user?.role ?? "employee") as Role))
        .filter((r) => r.title.toLocaleLowerCase("tr-TR").includes(query.trim().toLocaleLowerCase("tr-TR")))
        .map((r) => ({ label: r.title, hint: "Sayfaya git", icon: navIcons[r.key] || "fa-compass", path: r.path }));

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      const isTyping = ["INPUT", "TEXTAREA", "SELECT"].includes(document.activeElement?.tagName ?? "");
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        inputRef.current?.focus();
      } else if (event.key === "/" && !isTyping) {
        event.preventDefault();
        inputRef.current?.focus();
      } else if (event.key === "Escape") {
        setOpen(false);
      }
    };
    const onClick = (event: MouseEvent) => {
      if (!wrapRef.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener("keydown", onKey);
    document.addEventListener("click", onClick);
    return () => {
      document.removeEventListener("keydown", onKey);
      document.removeEventListener("click", onClick);
    };
  }, []);

  const select = (item: Item) => {
    navigate(item.path);
    setQuery("");
    setOpen(false);
  };

  return (
    <div className="header-search" ref={wrapRef}>
      <i aria-hidden="true" className="fa-solid fa-magnifying-glass" />
      <label className="sr-only" htmlFor="global-search-input">Personel, aksiyon veya sayfa ara</label>
      <input
        id="global-search-input"
        ref={inputRef}
        type="text"
        placeholder="Ara: personel, aksiyon, sayfa… (Ctrl+K)"
        autoComplete="off"
        role="combobox"
        aria-expanded={open && items.length > 0}
        aria-controls="global-search-results"
        value={query}
        onChange={(e) => { setQuery(e.target.value); setOpen(true); setActiveIndex(0); }}
        onFocus={() => setOpen(true)}
        onKeyDown={(e) => {
          if (e.key === "ArrowDown") { e.preventDefault(); setActiveIndex((i) => Math.min(i + 1, items.length - 1)); }
          if (e.key === "ArrowUp") { e.preventDefault(); setActiveIndex((i) => Math.max(i - 1, 0)); }
          if (e.key === "Enter" && items[activeIndex]) select(items[activeIndex]);
        }}
      />
      <div id="global-search-results" className="search-results" role="listbox" aria-label="Arama sonuçları" hidden={!open || items.length === 0}>
        {items.map((item, index) => (
          <button
            key={item.path}
            type="button"
            className={`search-result ${index === activeIndex ? "active" : ""}`}
            role="option"
            aria-selected={index === activeIndex}
            onClick={() => select(item)}
          >
            <i aria-hidden="true" className={`fa-solid ${item.icon}`} />
            <span>{item.label}</span>
            <small>{item.hint}</small>
          </button>
        ))}
      </div>
    </div>
  );
}
