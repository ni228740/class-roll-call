import math
import random
import sys
import tkinter as tk
from pathlib import Path
from tkinter import filedialog, messagebox, simpledialog, ttk


APP_NAME = "班级点名器"
if getattr(sys, "frozen", False):
    ROOT_DIR = Path(sys.executable).resolve().parent
else:
    ROOT_DIR = Path(__file__).resolve().parent.parent
DATA_DIR = ROOT_DIR / "name"


THEMES = {
    "春日樱花": {
        "top": "#fff2f8",
        "bottom": "#e2f8f0",
        "card": "#ffffff",
        "card_alt": "#fff6fa",
        "border": "#f4bed5",
        "text": "#3c2e3e",
        "muted": "#7e677e",
        "accent": "#e25c8e",
        "accent2": "#43a88a",
        "soft": "#ffd6e7",
        "season": "spring",
    },
    "夏日海风": {
        "top": "#e6f9ff",
        "bottom": "#fff4cd",
        "card": "#fffffa",
        "card_alt": "#ecfbff",
        "border": "#95d5e2",
        "text": "#243f4e",
        "muted": "#597480",
        "accent": "#0f97b0",
        "accent2": "#efa937",
        "soft": "#c0eef6",
        "season": "summer",
    },
    "秋日枫糖": {
        "top": "#fff5e3",
        "bottom": "#eeedff",
        "card": "#fffdf8",
        "card_alt": "#fff7eb",
        "border": "#e2b780",
        "text": "#413128",
        "muted": "#7c5b44",
        "accent": "#d6692e",
        "accent2": "#5d69b8",
        "soft": "#ffdaaa",
        "season": "autumn",
    },
    "冬日初雪": {
        "top": "#eef7ff",
        "bottom": "#f5f2ff",
        "card": "#ffffff",
        "card_alt": "#f1f7ff",
        "border": "#b3cce7",
        "text": "#2b3b52",
        "muted": "#5c6f8b",
        "accent": "#4984cd",
        "accent2": "#bc71ad",
        "soft": "#d7ebff",
        "season": "winter",
    },
}


def default_names():
    return [str(index) for index in range(1, 51)]


def parse_names(text):
    raw = text.replace("，", ",").replace("；", ";").replace("\t", "\n")
    for mark in [",", ";"]:
        raw = raw.replace(mark, "\n")
    names = []
    seen = set()
    for line in raw.splitlines():
        name = line.strip()
        if name and name not in seen:
            names.append(name)
            seen.add(name)
    return names


def ensure_data_files():
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    default_file = DATA_DIR / "默认名单_1-50.txt"
    if not default_file.exists():
        default_file.write_text("\n".join(default_names()), encoding="utf-8")


_SEASON_KEY = {"春日樱花": "spring", "夏日海风": "summer", "秋日枫糖": "autumn", "冬日初雪": "winter"}


def _icon_pixel(season, x, y, cx, cy):
    dx, dy = x - cx, y - cy
    d2 = dx * dx + dy * dy
    if season == "spring":
        if d2 <= 100:
            return "#FFB0C8" if d2 > 25 else "#E25C8E"
    elif season == "summer":
        if d2 <= 49:
            return "#FFD058"
        ax, ay = abs(dx), abs(dy)
        if (ax == 0 or ay == 0 or ax == ay) and 8 <= max(ax, ay) <= 11:
            return "#FFA500"
    elif season == "autumn":
        if abs(dx) + abs(dy) <= 10:
            return "#E06A2C"
        if dx == 0 and 5 <= dy <= 11:
            return "#8B4513"
    else:
        d = math.sqrt(d2) if d2 > 0 else 0
        if 1 <= d <= 10:
            angle = math.atan2(dy, dx) % (math.pi / 3)
            if angle < 0.2 or angle > (math.pi / 3 - 0.2):
                return "#4984CD"
        if d < 2.5:
            return "#FFFFFF"
    return ""


def _make_season_icon(root, season):
    size = 24
    cx = cy = size // 2
    img = tk.PhotoImage(master=root, width=size, height=size)
    for y in range(size):
        for x in range(size):
            color = _icon_pixel(season, x, y, cx, cy)
            if color:
                img.put(color, to=(x, y))
    return img


class RollCallState:
    def __init__(self):
        self.names = default_names()
        self.remaining = list(self.names)
        self.history = []
        self.with_replacement = True

    def set_names(self, names):
        cleaned = []
        seen = set()
        for name in names:
            value = str(name).strip()
            if value and value not in seen:
                cleaned.append(value)
                seen.add(value)
        cleaned = cleaned or default_names()
        if cleaned == self.names:
            return
        self.names = cleaned
        self.reset_remaining()

    def reset_remaining(self):
        self.remaining = list(self.names)

    def pool(self):
        if self.with_replacement:
            return self.names
        return self.remaining

    def peek(self):
        pool = self.pool()
        if pool:
            return random.choice(pool)
        return "空名单" if self.with_replacement else "已点完"

    def draw_one(self):
        if not self.names:
            return "空名单"
        if self.with_replacement:
            picked = random.choice(self.names)
        else:
            if not self.remaining:
                return "已经全部点完"
            index = random.randrange(len(self.remaining))
            picked = self.remaining.pop(index)
        self.history.insert(0, picked)
        del self.history[80:]
        return picked

    def lucky_star(self):
        if not self.names:
            return "空名单"
        picked = random.choice(self.names)
        self.history.insert(0, "幸运星：" + picked)
        del self.history[80:]
        return picked

    def draw_many(self, count):
        result = []
        for _ in range(count):
            picked = self.draw_one()
            if picked in ("已经全部点完", "空名单"):
                break
            result.append(picked)
        return result

    def groups(self, group_size):
        names = list(self.names)
        random.shuffle(names)
        return [names[index:index + group_size] for index in range(0, len(names), group_size)]


class AnimeButton(tk.Button):
    def __init__(self, master, app, primary=False, **kwargs):
        self.app = app
        self.primary = primary
        super().__init__(master, relief="flat", bd=0, cursor="hand2", padx=10, pady=7, **kwargs)
        self.apply_theme()

    def apply_theme(self):
        theme = self.app.theme
        bg = theme["accent"] if self.primary else theme["soft"]
        fg = "#ffffff" if self.primary else theme["text"]
        self.configure(
            bg=bg,
            fg=fg,
            activebackground=theme["accent2"],
            activeforeground="#ffffff",
            font=("Microsoft YaHei UI", 10, "bold"),
        )


class RollCallApp(tk.Tk):
    def __init__(self):
        super().__init__()
        ensure_data_files()
        self.state = RollCallState()
        self.theme_name = tk.StringVar(value="春日樱花")
        self.theme = THEMES[self.theme_name.get()]
        self.mode_var = tk.StringVar(value="with")
        self.rolling = False
        self.roll_frame = 0
        self.countdown_value = 0

        self.title(APP_NAME)
        self.geometry("1000x660")
        self.minsize(900, 600)
        self.configure(bg=self.theme["bottom"])

        self.canvas = tk.Canvas(self, highlightthickness=0, bd=0)
        self.canvas.place(relx=0, rely=0, relwidth=1, relheight=1)

        self.main = tk.Frame(self, bg=self.theme["bottom"])
        self.main.place(relx=0, rely=0, relwidth=1, relheight=1)

        self.build_ui()
        self.load_file_list()
        self._icons = {}
        for key in ("spring", "summer", "autumn", "winter"):
            self._icons[key] = _make_season_icon(self, key)
        self._set_season_icon()
        self.apply_theme()
        self.bind("<Configure>", lambda _event: self.paint_background())

    def build_ui(self):
        header = tk.Frame(self.main, bg=self.theme["bottom"])
        header.pack(fill="x", padx=20, pady=(16, 8))

        title_box = tk.Frame(header, bg=self.theme["bottom"])
        title_box.pack(side="left", fill="x", expand=True)
        self.title_label = tk.Label(title_box, text=APP_NAME, font=("Microsoft YaHei UI", 24, "bold"))
        self.title_label.pack(anchor="w")
        self.subtitle_label = tk.Label(title_box, text="随机、公平、轻量；适合课堂投屏时快速点名", font=("Microsoft YaHei UI", 10))
        self.subtitle_label.pack(anchor="w")

        self.theme_combo = ttk.Combobox(header, textvariable=self.theme_name, values=list(THEMES), state="readonly", width=12)
        self.theme_combo.pack(side="left", padx=(8, 12))
        self.theme_combo.bind("<<ComboboxSelected>>", lambda _event: self.change_theme())
        self.mini_button = AnimeButton(header, self, text="缩小模式", primary=True, command=self.open_mini)
        self.mini_button.pack(side="left")
        self.exit_button = AnimeButton(header, self, text="退出程序", command=self.destroy)
        self.exit_button.pack(side="left", padx=(8, 0))

        body = tk.Frame(self.main, bg=self.theme["bottom"])
        body.pack(fill="both", expand=True, padx=20, pady=(4, 20))
        body.columnconfigure(0, weight=30, uniform="cols")
        body.columnconfigure(1, weight=45, uniform="cols")
        body.columnconfigure(2, weight=25, uniform="cols")
        body.rowconfigure(0, weight=1)

        self.settings_card = self.card(body)
        self.settings_card.grid(row=0, column=0, sticky="nsew", padx=(0, 8))
        self.draw_card = self.card(body)
        self.draw_card.grid(row=0, column=1, sticky="nsew", padx=8)
        self.fun_card = self.card(body)
        self.fun_card.grid(row=0, column=2, sticky="nsew", padx=(8, 0))

        self.build_settings()
        self.build_draw()
        self.build_fun()

    def card(self, master):
        frame = tk.Frame(master, bd=1, relief="solid", padx=14, pady=14)
        return frame

    def build_settings(self):
        self.list_label = tk.Label(self.settings_card, text="选择名单", anchor="w")
        self.list_label.pack(fill="x")
        row = tk.Frame(self.settings_card)
        row.pack(fill="x", pady=(6, 10))
        self.file_var = tk.StringVar()
        self.file_combo = ttk.Combobox(row, textvariable=self.file_var, state="readonly")
        self.file_combo.pack(side="left", fill="x", expand=True)
        self.file_combo.bind("<<ComboboxSelected>>", lambda _event: self.load_selected_file())
        self.refresh_button = AnimeButton(row, self, text="刷新", command=self.load_file_list)
        self.refresh_button.pack(side="left", padx=(8, 0))

        button_grid = tk.Frame(self.settings_card)
        button_grid.pack(fill="x", pady=(0, 10))
        self.import_button = AnimeButton(button_grid, self, text="导入 txt", command=self.import_file)
        self.import_button.grid(row=0, column=0, sticky="ew", padx=(0, 4), pady=3)
        self.save_button = AnimeButton(button_grid, self, text="保存名单", primary=True, command=self.save_file)
        self.save_button.grid(row=0, column=1, sticky="ew", padx=(4, 0), pady=3)
        self.default_button = AnimeButton(button_grid, self, text="默认 1-50", command=self.use_default)
        self.default_button.grid(row=1, column=0, sticky="ew", padx=(0, 4), pady=3)
        self.apply_button = AnimeButton(button_grid, self, text="应用编辑", primary=True, command=self.apply_names)
        self.apply_button.grid(row=1, column=1, sticky="ew", padx=(4, 0), pady=3)
        button_grid.columnconfigure(0, weight=1)
        button_grid.columnconfigure(1, weight=1)

        self.names_label = tk.Label(self.settings_card, text="编辑名单（一行一个，也支持逗号分隔）", anchor="w")
        self.names_label.pack(fill="x", pady=(4, 6))
        self.names_text = tk.Text(self.settings_card, height=18, wrap="none", font=("Microsoft YaHei UI", 10), bd=1, relief="solid")
        self.names_text.pack(fill="both", expand=True)
        self.names_text.insert("1.0", "\n".join(default_names()))
        self.names_text.bind("<KeyRelease>", lambda _event: self.update_count_label())
        self.count_label = tk.Label(self.settings_card, text="", anchor="w")
        self.count_label.pack(fill="x", pady=(8, 0))

    def build_draw(self):
        top = tk.Frame(self.draw_card)
        top.pack(fill="x")
        self.draw_title = tk.Label(top, text="今日登场", font=("Microsoft YaHei UI", 14, "bold"))
        self.draw_title.pack(anchor="w")

        mode_row = tk.Frame(self.draw_card)
        mode_row.pack(fill="x", pady=(8, 10))
        self.with_radio = tk.Radiobutton(mode_row, text="放回点名", variable=self.mode_var, value="with", command=self.change_mode)
        self.with_radio.pack(side="left")
        self.without_radio = tk.Radiobutton(mode_row, text="不放回点名", variable=self.mode_var, value="without", command=self.change_mode)
        self.without_radio.pack(side="left", padx=(14, 0))

        self.result_label = tk.Label(self.draw_card, text="准备开始", anchor="center", font=("Microsoft YaHei UI", 48, "bold"))
        self.result_label.pack(fill="both", expand=True, pady=10)
        self.pool_label = tk.Label(self.draw_card, text="", anchor="center", font=("Microsoft YaHei UI", 10))
        self.pool_label.pack(fill="x")
        self.start_button = AnimeButton(self.draw_card, self, text="开始点名", primary=True, command=self.start_roll)
        self.start_button.configure(font=("Microsoft YaHei UI", 16, "bold"), pady=10)
        self.start_button.pack(fill="x", pady=(14, 8))
        self.reset_button = AnimeButton(self.draw_card, self, text="重置未点名单", command=self.reset_remaining)
        self.reset_button.pack(fill="x")

    def build_fun(self):
        self.fun_title = tk.Label(self.fun_card, text="趣味玩法", font=("Microsoft YaHei UI", 14, "bold"))
        self.fun_title.pack(anchor="w")

        self.multi_spin = self.spin_row("连抽人数", 1, 20, 3)
        self.multi_button = AnimeButton(self.fun_card, self, text="连抽", primary=True, command=self.draw_many)
        self.multi_button.pack(fill="x", pady=(6, 12))

        self.group_spin = self.spin_row("每组人数", 2, 12, 4)
        self.group_button = AnimeButton(self.fun_card, self, text="随机分组", primary=True, command=self.show_groups)
        self.group_button.pack(fill="x", pady=(6, 12))

        self.lucky_button = AnimeButton(self.fun_card, self, text="幸运星", command=self.lucky_star)
        self.lucky_button.pack(fill="x", pady=(0, 8))
        self.countdown_button = AnimeButton(self.fun_card, self, text="3 秒倒计时", command=self.start_countdown)
        self.countdown_button.pack(fill="x", pady=(0, 14))

        self.history_label = tk.Label(self.fun_card, text="点名记录", anchor="w")
        self.history_label.pack(fill="x")
        self.history_list = tk.Listbox(self.fun_card, bd=1, relief="solid", height=8)
        self.history_list.pack(fill="both", expand=True, pady=(6, 8))
        self.clear_history_button = AnimeButton(self.fun_card, self, text="清空记录", command=self.clear_history)
        self.clear_history_button.pack(fill="x")

    def spin_row(self, label, start, end, value):
        row = tk.Frame(self.fun_card)
        row.pack(fill="x", pady=(14, 0))
        tk.Label(row, text=label).pack(side="left")
        spin = tk.Spinbox(row, from_=start, to=end, width=5, justify="center")
        spin.delete(0, "end")
        spin.insert(0, str(value))
        spin.pack(side="right")
        return spin

    def change_theme(self):
        self.theme = THEMES[self.theme_name.get()]
        self.apply_theme()
        self._set_season_icon()

    def _set_season_icon(self):
        key = _SEASON_KEY.get(self.theme_name.get(), "spring")
        icon = self._icons.get(key)
        if icon:
            self.iconphoto(True, icon)

    def apply_theme(self):
        theme = self.theme
        self.configure(bg=theme["bottom"])
        self.main.configure(bg=theme["bottom"])
        for widget in self.main.winfo_children():
            self.retheme_tree(widget)
        self.title_label.configure(fg=theme["text"], bg=theme["bottom"])
        self.subtitle_label.configure(fg=theme["muted"], bg=theme["bottom"])
        self.result_label.configure(fg=theme["accent"])
        for button in self.find_buttons(self):
            button.apply_theme()
        self.paint_background()
        self.update_count_label()
        self.update_stats()

    def retheme_tree(self, widget):
        theme = self.theme
        if isinstance(widget, tk.Frame):
            if widget in (self.settings_card, self.draw_card, self.fun_card):
                widget.configure(bg=theme["card"], highlightbackground=theme["border"])
            else:
                widget.configure(bg=theme["bottom"] if widget.master is self.main else theme["card"])
        elif isinstance(widget, tk.Label):
            widget.configure(bg=widget.master.cget("bg"), fg=theme["text"])
        elif isinstance(widget, tk.Text):
            widget.configure(bg=theme["card_alt"], fg=theme["text"], insertbackground=theme["text"])
        elif isinstance(widget, tk.Listbox):
            widget.configure(bg=theme["card_alt"], fg=theme["text"])
        elif isinstance(widget, (tk.Radiobutton, tk.Spinbox)):
            widget.configure(bg=widget.master.cget("bg"), fg=theme["text"], activebackground=widget.master.cget("bg"))
        for child in widget.winfo_children():
            self.retheme_tree(child)

    def find_buttons(self, widget):
        for child in widget.winfo_children():
            if isinstance(child, AnimeButton):
                yield child
            yield from self.find_buttons(child)

    def paint_background(self):
        self.canvas.delete("all")
        width = max(1, self.winfo_width())
        height = max(1, self.winfo_height())
        theme = self.theme
        self.canvas.create_rectangle(0, 0, width, height, fill=theme["bottom"], outline="")
        season = theme["season"]
        if season == "spring":
            self.canvas.create_line(-30, 86, width * 0.3, 10, width + 35, 45, smooth=True, fill="#a97888", width=3)
            for i in range(64):
                x = (i * 67 + 23) % width
                y = (i * 43 + 19) % height
                self.canvas.create_oval(x, y, x + 13, y + 8, fill="#ffc7dc", outline="")
        elif season == "summer":
            self.canvas.create_oval(width - 118, 22, width - 40, 100, fill="#ffd058", outline="")
            for row in range(4):
                y = height - 64 + row * 14
                self.canvas.create_line(-20, y, width * 0.3, y - 22, width * 0.65, y + 22, width + 20, y, smooth=True, fill="#36afc7", width=3)
        elif season == "autumn":
            for row in range(5):
                y = 70 + row * 44
                self.canvas.create_line(-20, y, width * 0.35, y - 22, width * 0.7, y + 22, width + 20, y - 12, smooth=True, fill="#9c84c4", width=2)
            for i in range(56):
                x = (i * 59 + 41) % width
                y = (i * 71 + 15) % height
                color = "#e06a2c" if i % 2 else "#8f69be"
                self.canvas.create_polygon(x, y - 7, x + 8, y, x + 2, y + 8, x - 6, y + 4, fill=color, outline="")
        else:
            self.canvas.create_arc(-40, height - 145, width + 50, height + 80, start=10, extent=160, fill="#ddecff", outline="#8ab2df")
            for i in range(82):
                x = (i * 47 + 11) % width
                y = (i * 61 + 17) % height
                size = 3 + i % 3
                self.canvas.create_oval(x, y, x + size, y + size, fill="#ffffff", outline="")

    def load_file_list(self):
        ensure_data_files()
        files = sorted(DATA_DIR.glob("*.txt"), key=lambda item: item.name)
        self.file_combo["values"] = [item.stem for item in files]
        self.file_paths = {item.stem: item for item in files}
        if files:
            target = "默认名单_1-50" if "默认名单_1-50" in self.file_paths else files[0].stem
            self.file_var.set(target)
            self.load_selected_file()

    def load_selected_file(self):
        path = getattr(self, "file_paths", {}).get(self.file_var.get())
        if not path:
            return
        names = parse_names(path.read_text(encoding="utf-8"))
        if not names:
            messagebox.showinfo("名单为空", "这个名单文件是空的，已保留当前名单。")
            return
        self.set_editor_names(names)
        self.apply_names(show_message=False)

    def import_file(self):
        path = filedialog.askopenfilename(title="选择名单文件", filetypes=[("文本名单", "*.txt *.csv"), ("所有文件", "*.*")])
        if not path:
            return
        names = parse_names(Path(path).read_text(encoding="utf-8"))
        if not names:
            messagebox.showwarning("导入失败", "没有读到有效名字。")
            return
        self.set_editor_names(names)
        self.apply_names()

    def save_file(self):
        names = parse_names(self.names_text.get("1.0", "end"))
        if not names:
            messagebox.showinfo("无法保存", "请先输入至少一个名字。")
            return
        path = filedialog.asksaveasfilename(
            title="保存名单",
            initialdir=str(DATA_DIR),
            initialfile="我的班级名单.txt",
            defaultextension=".txt",
            filetypes=[("文本名单", "*.txt")],
        )
        if not path:
            return
        Path(path).write_text("\n".join(names), encoding="utf-8")
        self.load_file_list()

    def use_default(self):
        self.set_editor_names(default_names())
        self.apply_names()

    def set_editor_names(self, names):
        self.names_text.delete("1.0", "end")
        self.names_text.insert("1.0", "\n".join(names))
        self.update_count_label()

    def apply_names(self, show_message=True):
        names = parse_names(self.names_text.get("1.0", "end"))
        self.state.set_names(names)
        if show_message:
            self.show_result("名单已应用")
        self.update_stats()

    def change_mode(self):
        self.state.with_replacement = self.mode_var.get() == "with"
        if not self.state.with_replacement:
            self.state.reset_remaining()
        self.update_stats()

    def reset_remaining(self):
        self.state.reset_remaining()
        self.show_result("已重置")
        self.update_stats()

    def update_count_label(self):
        count = len(parse_names(self.names_text.get("1.0", "end"))) if hasattr(self, "names_text") else 0
        if hasattr(self, "count_label"):
            self.count_label.configure(text=f"当前编辑区：{count} 人", fg=self.theme["muted"])

    def update_stats(self):
        if not hasattr(self, "pool_label"):
            return
        if self.state.with_replacement:
            text = f"当前名单 {len(self.state.names)} 人；放回模式每次都从全名单抽取"
        else:
            text = f"当前名单 {len(self.state.names)} 人；未点 {len(self.state.remaining)} 人"
        self.pool_label.configure(text=text, fg=self.theme["muted"])

    def start_roll(self):
        if self.rolling:
            self.stop_roll()
            return
        self.apply_names(show_message=False)
        self.rolling = True
        self.roll_frame = 0
        self.start_button.configure(text="停止点名")
        self.tick_roll()

    def tick_roll(self):
        self.roll_frame += 1
        if not self.rolling:
            return
        self.show_result(self.state.peek(), self.theme["accent2"])
        self.after(40, self.tick_roll)

    def stop_roll(self):
        picked = self.state.draw_one()
        self.show_result(picked, self.theme["accent"])
        self.rolling = False
        self.start_button.configure(text="开始点名")
        self.update_history()
        self.update_stats()

    def show_result(self, text, color=None):
        font_size = 48
        if len(text) > 8:
            font_size = 34
        if len(text) > 16:
            font_size = 24
        self.result_label.configure(text=text, fg=color or self.theme["accent"], font=("Microsoft YaHei UI", font_size, "bold"))

    def draw_many(self):
        self.apply_names(show_message=False)
        count = int(self.multi_spin.get())
        result = self.state.draw_many(count)
        self.show_result("、".join(result) if result else "请重置名单")
        self.update_history()
        self.update_stats()

    def show_groups(self):
        self.apply_names(show_message=False)
        size = int(self.group_spin.get())
        lines = [f"第 {index + 1} 组：" + "、".join(group) for index, group in enumerate(self.state.groups(size))]
        messagebox.showinfo("随机分组", "\n".join(lines))

    def lucky_star(self):
        self.apply_names(show_message=False)
        self.show_result("幸运星：" + self.state.lucky_star())
        self.update_history()

    def start_countdown(self):
        if self.rolling:
            return
        self.countdown_value = 3
        self.tick_countdown()

    def tick_countdown(self):
        if self.countdown_value > 0:
            self.show_result(str(self.countdown_value), self.theme["accent2"])
            self.countdown_value -= 1
            self.after(760, self.tick_countdown)
            return
        self.start_roll()

    def update_history(self):
        self.history_list.delete(0, "end")
        for index, item in enumerate(self.state.history, start=1):
            self.history_list.insert("end", f"{index:02d}. {item}")

    def clear_history(self):
        self.state.history.clear()
        self.update_history()

    def open_mini(self):
        if self.rolling:
            self.stop_roll()
        self.apply_names(show_message=False)
        MiniWindow(self, self.state)
        self.withdraw()


class MiniWindow(tk.Toplevel):
    def __init__(self, app, state):
        super().__init__(app)
        self.app = app
        self.state = state
        self.theme = app.theme
        self.rolling = False
        self.frame = 0
        self.drag_x = 0
        self.drag_y = 0

        self.overrideredirect(True)
        self.attributes("-topmost", True)
        self.geometry("158x158")
        self.configure(bg=self.theme["soft"], highlightthickness=2, highlightbackground=self.theme["border"])
        self.label = tk.Label(self, text="开始", font=("Microsoft YaHei UI", 22, "bold"), bg=self.theme["soft"], fg=self.theme["text"], cursor="hand2")
        self.label.place(x=0, y=0, width=158, height=158)
        self.status_label = tk.Label(
            self,
            text="",
            bg=self.theme["soft"],
            fg=self.theme["muted"],
            font=("Microsoft YaHei UI", 8, "bold"),
            cursor="hand2",
        )
        self.status_label.place_forget()
        self.expand_button = tk.Button(
            self,
            text="放大",
            command=self.close,
            relief="flat",
            bd=0,
            bg=self.theme["soft"],
            fg=self.theme["text"],
            activebackground=self.theme["accent2"],
            activeforeground="#ffffff",
            font=("Microsoft YaHei UI", 8, "bold"),
            cursor="hand2",
        )
        self.expand_button.place(x=106, y=7, width=45, height=24)
        self.label.bind("<ButtonPress-1>", self.start_drag)
        self.label.bind("<B1-Motion>", self.drag)
        self.label.bind("<ButtonRelease-1>", self.on_release)
        self.label.bind("<ButtonPress-3>", self.show_menu)
        self.status_label.bind("<ButtonPress-1>", self.start_drag)
        self.status_label.bind("<B1-Motion>", self.drag)
        self.status_label.bind("<ButtonRelease-1>", self.on_release)
        self.status_label.bind("<ButtonPress-3>", self.show_menu)

        self.menu = tk.Menu(self, tearoff=False)
        self.menu.add_command(label="放大", command=self.close)
        self.menu.add_command(label="重置未点名单", command=self.reset_remaining)
        self.menu.add_command(label="退出程序", command=self.app.destroy)
        icon = getattr(app, "_icons", {}).get(self.theme.get("season", "spring"))
        if icon:
            self.iconphoto(True, icon)
        self.protocol("WM_DELETE_WINDOW", self.close)
        self.update_status()

    def start_drag(self, event):
        self.drag_x = event.x
        self.drag_y = event.y
        self._press_rx = event.x_root
        self._press_ry = event.y_root

    def drag(self, event):
        x = self.winfo_x() + event.x - self.drag_x
        y = self.winfo_y() + event.y - self.drag_y
        self.geometry(f"+{x}+{y}")

    def on_release(self, event):
        dx = abs(event.x_root - getattr(self, "_press_rx", event.x_root))
        dy = abs(event.y_root - getattr(self, "_press_ry", event.y_root))
        if dx < 5 and dy < 5:
            self.start_roll(event)

    def show_menu(self, event):
        self.menu.tk_popup(event.x_root, event.y_root)

    def reset_remaining(self):
        self.state.reset_remaining()
        self.set_text("已重置", self.theme["accent"])
        self.update_status()

    def start_roll(self, _event=None):
        if self.rolling:
            self.stop_roll()
            return
        self.rolling = True
        self.frame = 0
        self.tick()

    def tick(self):
        self.frame += 1
        if not self.rolling:
            return
        self.set_text(self.state.peek(), self.theme["accent2"])
        self.after(40, self.tick)

    def stop_roll(self):
        self.rolling = False
        self.set_text(self.state.draw_one(), self.theme["accent"])
        self.update_status()

    def update_status(self):
        if self.state.with_replacement:
            self.label.place(x=0, y=0, width=158, height=158)
            self.status_label.place_forget()
            self.status_label.configure(text="")
        else:
            self.label.place(x=0, y=0, width=158, height=132)
            self.status_label.place(x=0, y=132, width=158, height=26)
            self.status_label.configure(text=f"未点 {len(self.state.remaining)}/{len(self.state.names)}")
        self.expand_button.lift()

    def set_text(self, text, color):
        size = 22
        if len(text) > 4:
            size = 16
        if len(text) > 8:
            size = 12
        self.label.configure(text=text, fg=color, font=("Microsoft YaHei UI", size, "bold"))

    def close(self):
        self.destroy()
        self.app.deiconify()
        self.app.update_stats()
        self.app.update_history()


if __name__ == "__main__":
    RollCallApp().mainloop()
