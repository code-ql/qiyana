'use client'

import * as React from 'react'

const screenshots = [
    {
        title: '我的战绩面板',
        tag: 'Dashboard',
        desc: '自动识别当前账号，查看近期对局、胜负状态、模式筛选和详细战绩。',
        src: '/qiyana/1.png',
    },
    {
        title: '战绩查询',
        tag: 'Search',
        desc: '搜索召唤师并保留多标签结果，赛前快速查看队友或对手表现。',
        src: '/qiyana/2.png',
    },
    {
        title: '对局监听',
        tag: 'Live',
        desc: '英雄选择阶段读取队友段位、近期表现、补位状态和常用英雄。',
        src: '/qiyana/3_1.png',
    },
    {
        title: '实时阵容信息',
        tag: 'In Game',
        desc: '进入游戏后继续展示关键数据，帮助判断阵容强弱和玩家状态。',
        src: '/qiyana/3_2.png',
    },
    {
        title: '设置与额外功能',
        tag: 'Settings',
        desc: '设置页面提供自动接受对局等额外功能，减少重复操作，让客户端体验更顺手。',
        src: '/qiyana/4.png',
    },
]

const features = [
    ['01', '本机 LCU 连接', '连接已登录的英雄联盟客户端，不需要输入账号密码。'],
    ['02', '战绩面板', '把近期战绩、对局详情和模式筛选集中到一个页面。'],
    ['03', '对局监听', '从英雄选择到游戏中持续读取关键玩家信息。'],
    ['04', '额外功能', '设置页面提供自动接受对局等便捷能力，减少重复操作。'],
]

export default function Page() {
    const [preview, setPreview] = React.useState<(typeof screenshots)[number] | null>(null)

    return (
        <main className="h-full min-h-0 overflow-hidden rounded-[2rem] bg-[#eef2f7] text-[#111827]">
            <div className="flex h-full min-h-0 flex-col overflow-hidden rounded-[2rem] border border-black/10 bg-[linear-gradient(135deg,#f8fafc_0%,#eef2ff_45%,#e0f2fe_100%)]">
                <header className="shrink-0 px-4 pt-4 sm:px-6">
                    <div className="flex items-center justify-between rounded-[1.5rem] border border-white bg-white/80 px-4 py-3 shadow-sm backdrop-blur-xl">
                        <div className="flex items-center gap-3">
                            <div className="grid h-11 w-11 place-items-center rounded-2xl bg-[#111827] text-lg font-black text-white shadow-lg shadow-slate-300">Q</div>
                            <div>
                                <p className="text-xs font-bold uppercase tracking-[0.28em] text-sky-600">League LCU Client</p>
                                <h1 className="text-lg font-black tracking-tight">Qiyana</h1>
                            </div>
                        </div>
                        <a
                            href="/download/qiyana.exe"
                            className="rounded-2xl bg-[#111827] px-5 py-2.5 text-sm font-black text-white shadow-lg shadow-slate-300 transition hover:-translate-y-0.5 hover:bg-sky-600"
                        >
                            下载 Windows 版
                        </a>
                    </div>
                </header>

                <div className="grid min-h-0 flex-1 gap-4 overflow-hidden px-4 pb-4 pt-4 lg:grid-cols-[360px_1fr] sm:px-6 sm:pb-6">
                    <aside className="min-h-0 overflow-y-auto rounded-[2rem] bg-white p-6 shadow-sm">
                        <div className="inline-flex rounded-full bg-sky-50 px-4 py-2 text-xs font-black text-sky-700 ring-1 ring-sky-100">
                            本机读取 · 战绩查询 · 对局监听
                        </div>
                        <h2 className="mt-6 text-4xl font-black leading-[1.05] tracking-tight">
                            一个更干净的英雄联盟数据入口
                        </h2>
                        <p className="mt-5 text-sm leading-7 text-slate-600">
                            Qiyana 连接已登录的英雄联盟客户端，把我的战绩面板、战绩查询、英雄选择监听和游戏中阵容信息整理成清晰的卡片视图。
                        </p>

                        <div className="mt-7 grid gap-3">
                            {features.map(([num, title, desc]) => (
                                <div key={title} className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                                    <div className="mb-3 grid h-10 w-10 place-items-center rounded-2xl bg-white text-sm font-black text-sky-600 shadow-sm">{num}</div>
                                    <h3 className="font-black">{title}</h3>
                                    <p className="mt-1 text-sm leading-6 text-slate-600">{desc}</p>
                                </div>
                            ))}
                        </div>

                        <div className="mt-7 grid gap-3 rounded-3xl bg-[#111827] p-5 text-white">
                            <p className="text-xs font-black uppercase tracking-[0.2em] text-sky-300">No Password</p>
                            <div className="text-5xl font-black">0</div>
                            <p className="text-sm leading-6 text-slate-300">不需要输入英雄联盟账号密码，只读取本机已登录客户端数据。</p>
                            <a
                                href="/download/qiyana.exe"
                                className="mt-2 rounded-2xl bg-white px-5 py-3 text-center text-sm font-black text-[#111827] transition hover:bg-sky-100"
                            >
                                下载 Windows 版
                            </a>
                        </div>
                    </aside>

                    <section id="preview" className="min-h-0 overflow-y-auto rounded-[2rem] bg-white/55 p-3 shadow-sm ring-1 ring-white/80">
                        <div className="grid gap-3 xl:grid-cols-2">
                            {screenshots.map((item, index) => (
                                <article
                                    key={item.src}
                                    className={index === 0 ? 'overflow-hidden rounded-[2rem] bg-[#111827] p-3 shadow-sm xl:col-span-2' : 'overflow-hidden rounded-[2rem] bg-white p-3 shadow-sm'}
                                >
                                    <div className={index === 0 ? 'mb-3 flex items-center justify-between px-2 text-white' : 'mb-3 flex items-center justify-between px-2'}>
                                        <div>
                                            <p className={index === 0 ? 'text-xs font-black uppercase tracking-[0.2em] text-sky-300' : 'text-xs font-black uppercase tracking-[0.2em] text-sky-600'}>{item.tag}</p>
                                            <h3 className="text-lg font-black">{item.title}</h3>
                                        </div>
                                        <span className={index === 0 ? 'rounded-full bg-white/10 px-3 py-1 text-xs text-slate-200' : 'rounded-full bg-slate-100 px-3 py-1 text-xs font-bold text-slate-500'}>
                                            点击查看大图
                                        </span>
                                    </div>
                                    <button
                                        type="button"
                                        onClick={() => setPreview(item)}
                                        className={index === 0 ? 'group/image block aspect-[2017/1069] w-full rounded-[1.5rem] bg-slate-950 p-2 text-left outline-none ring-sky-400 transition focus:ring-2' : 'group/image block aspect-[2017/1069] w-full rounded-[1.5rem] bg-slate-100 p-2 text-left outline-none ring-sky-400 transition focus:ring-2'}
                                    >
                                        <img src={item.src} alt={`${item.title}截图`} className="h-full w-full rounded-[1.1rem] object-contain transition group-hover/image:scale-[1.01]" />
                                    </button>
                                    <p className={index === 0 ? 'px-2 pt-3 text-sm leading-6 text-slate-300' : 'px-2 pt-3 text-sm leading-6 text-slate-600'}>{item.desc}</p>
                                </article>
                            ))}
                        </div>
                    </section>
                </div>
            </div>

            {preview && (
                <div
                    className="fixed inset-0 z-50 flex min-h-0 items-center justify-center bg-slate-950/80 p-4 backdrop-blur-sm"
                    onClick={() => setPreview(null)}
                >
                    <div
                        className="flex max-h-full w-full max-w-7xl flex-col overflow-hidden rounded-[2rem] bg-white shadow-2xl"
                        onClick={(event) => event.stopPropagation()}
                    >
                        <div className="flex shrink-0 items-center justify-between gap-4 border-b border-slate-200 px-5 py-4">
                            <div>
                                <p className="text-xs font-black uppercase tracking-[0.24em] text-sky-600">{preview.tag}</p>
                                <h2 className="text-xl font-black text-slate-950">{preview.title}</h2>
                            </div>
                            <button
                                type="button"
                                onClick={() => setPreview(null)}
                                className="rounded-2xl bg-slate-100 px-4 py-2 text-sm font-black text-slate-700 transition hover:bg-slate-200"
                            >
                                关闭
                            </button>
                        </div>
                        <div className="min-h-0 flex-1 overflow-auto bg-slate-100 p-4">
                            <img
                                src={preview.src}
                                alt={`${preview.title}大图`}
                                className="mx-auto h-auto max-h-none w-full max-w-none rounded-2xl border border-slate-200 bg-white object-contain"
                            />
                        </div>
                    </div>
                </div>
            )}
        </main>
    )
}
