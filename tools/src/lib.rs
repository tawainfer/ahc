use anyhow::{anyhow, bail, ensure, Context, Result};
use rand::{seq::SliceRandom, Rng};
use rand_chacha::{rand_core::SeedableRng, ChaCha20Rng};
use std::collections::HashSet;
use std::{fmt, str};
use svg::node::element::SVG;

mod lib_vis;
use lib_vis::*;

#[cfg(target_arch = "wasm32")]
use wasm_bindgen::prelude::*;

#[cfg(target_arch = "wasm32")]
use once_cell::sync::Lazy;

#[cfg(target_arch = "wasm32")]
use std::sync::Mutex;

const N: usize = 600;
const T_SZ: usize = 600;
const AL_LB: usize = N;
const AL_UB: usize = N * 2;
const BL_LB_SQRT: usize = 2;
const BL_UB_SQRT: usize = 5;
const COORD_MIN: i64 = 0;
const COORD_MAX: i64 = 1000;
const MAX_OPERATION_CNT: usize = 100000;

fn read<T: Copy + PartialOrd + std::fmt::Display + std::str::FromStr>(
    token: Option<&str>,
    lb: T,
    ub: T,
) -> Result<T> {
    if let Some(v) = token {
        if let Ok(v) = v.parse::<T>() {
            if v < lb || ub < v {
                bail!("Out of range: {}", v);
            } else {
                Ok(v)
            }
        } else {
            bail!("Parse error: {}", v);
        }
    } else {
        bail!("Unexpected EOF");
    }
}

#[derive(Clone, Debug)]
pub struct Input {
    n: usize,
    m: usize,
    t_sz: usize,
    al: usize,
    bl: usize,
    edges: Vec<(usize, usize)>,
    t: Vec<usize>,
    coordinates: Vec<(i64, i64)>,
}

impl Input {
    pub fn new() -> Self {
        Input {
            n: 0,
            m: 0,
            t_sz: 0,
            al: 0,
            bl: 0,
            edges: vec![],
            t: vec![],
            coordinates: vec![],
        }
    }
}

impl fmt::Display for Input {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        writeln!(
            f,
            "{} {} {} {} {}",
            self.n, self.m, self.t_sz, self.al, self.bl
        )?;

        // print edges
        for &(u, v) in self.edges.iter() {
            writeln!(f, "{} {}", u, v)?;
        }

        // print targets
        for i in 0..self.t.len() {
            write!(f, "{}", self.t[i])?;
            if i == self.t_sz - 1 {
                writeln!(f)?;
            } else {
                write!(f, " ")?;
            }
        }

        // print coordinates
        for &(x, y) in self.coordinates.iter() {
            writeln!(f, "{} {}", x, y)?;
        }

        Ok(())
    }
}

fn parse_input(s: &str) -> Result<Input> {
    let mut tokens = s.split_whitespace();

    // You can use values that do not satisfy the constraints of the problem statement as input.
    // We do not guarantee the behavior when such values are used.
    let n: usize = read(tokens.next(), 0, usize::MAX).context("N")?;
    let m: usize = read(tokens.next(), n - 1, n * (n - 1)).context("M")?;
    let t_sz: usize = read(tokens.next(), 0, usize::MAX).context("T")?;
    let al: usize = read(tokens.next(), 0, usize::MAX).context("L_A")?;
    let bl = read(tokens.next(), 0, usize::MAX).context("L_B")?;

    let mut edges = vec![];
    for i in 0..m {
        let u: usize = read(tokens.next(), 0, n - 1).context(format!("u_{}", i))?;
        let v: usize = read(tokens.next(), 0, n - 1).context(format!("v_{}", i))?;
        edges.push((u, v));
    }

    let mut t = vec![];
    for i in 0..t_sz {
        let t_val = read(tokens.next(), 0, n - 1).context(format!("t_{}", i))?;
        t.push(t_val);
    }

    let mut coordinates = vec![];
    for i in 0..n {
        let x = read(tokens.next(), COORD_MIN, COORD_MAX).context(format!("x_{}", i))?;
        let y = read(tokens.next(), COORD_MIN, COORD_MAX).context(format!("y_{}", i))?;
        coordinates.push((x, y));
    }

    Ok(Input {
        n,
        m,
        t_sz,
        al,
        bl,
        edges,
        t,
        coordinates,
    })
}

#[derive(Clone, Copy)]
#[cfg_attr(target_arch = "wasm32", wasm_bindgen(getter_with_clone))]
pub struct CopySignals {
    pub len: usize,
    pub p_a: usize,
    pub p_b: usize,
}

#[derive(Clone, Copy)]
enum Op {
    CopySignals(CopySignals),
    Move(usize),
}

impl fmt::Display for Op {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match self {
            Op::CopySignals(cs) => writeln!(f, "s {} {} {}", cs.len, cs.p_a, cs.p_b)?,
            Op::Move(v) => writeln!(f, "m {}", v)?,
        }

        Ok(())
    }
}

#[allow(dead_code)]
#[derive(Clone)]
struct CommentedOp {
    op: Op,
    comments: Vec<String>,
}

#[derive(Clone)]
struct Output {
    initial_comments: Vec<String>,
    initial_a: Vec<usize>,
    commented_ops: Vec<CommentedOp>,
}

fn parse_output(s: &str, input: &Input) -> Result<Output> {
    let mut initial_comments = vec![];
    let mut initial_a = vec![];
    let mut commented_ops = vec![];
    let mut comments = vec![];

    let mut initialized_a = false;
    for (i, line) in s.trim().lines().enumerate() {
        let line = line.trim();

        if !line.is_empty() {
            if line.starts_with("#") {
                let comment = line.strip_prefix("#").unwrap().trim().to_string();
                comments.push(comment);
            } else if !initialized_a {
                // Set values of A
                let mut tokens = line.split_whitespace();
                for _ in 0..input.al {
                    let val: usize = read(tokens.next(), 0, input.n - 1)
                        .context(format!("{}-th line", i + 1))?;
                    initial_a.push(val)
                }
                initial_comments = comments.clone();
                comments.clear();
                initialized_a = true;
                ensure!(
                    tokens.next() == None,
                    format!("{}-th line must have {} elements.", i, input.al)
                );
            } else {
                let op = parse_op(line, input).context(format!("{}-th line", i + 1))?;
                commented_ops.push(CommentedOp {
                    op,
                    comments: comments.clone(),
                });
                comments.clear();
            }
        }
    }

    Ok(Output {
        initial_comments,
        initial_a,
        commented_ops,
    })
}

fn parse_op(line: &str, input: &Input) -> Result<Op> {
    let mut tokens = line.split_whitespace();
    let type_: char = read(tokens.next(), '\0', char::MAX).context("operation type")?;

    match type_ {
        's' => {
            let len: usize = read(tokens.next(), 1, input.n - 1).context("l")?;
            let p_a = read(tokens.next(), 0, input.al).context("P_A")?;
            let p_b = read(tokens.next(), 0, input.bl).context("P_B")?;
            ensure!(
                tokens.next() == None,
                "Operation s must be like 's, l, P_A, P_B'"
            );

            Ok(Op::CopySignals(CopySignals { len, p_a, p_b }))
        }
        'm' => {
            let v: usize = read(tokens.next(), 0, input.n - 1).context("v")?;
            ensure!(tokens.next() == None, "Operation m must be like 'm v'");
            Ok(Op::Move(v))
        }
        _ => Err(anyhow!("Invalid value: {}", type_).context("operation type"))?,
    }
}

#[derive(Clone)]
struct State {
    score: usize,
    op_cnt: usize,
    target_idx: usize,
    cur_v: usize,
    a: Vec<usize>,
    b: Vec<isize>,
    visited: Vec<usize>,

    traffic_light: Vec<usize>,
    // red: 0,
    // green: 1, 2, 3...
}

impl State {
    fn new(input: &Input, output: &Output) -> State {
        State {
            score: 0,
            op_cnt: 0,
            target_idx: 0,
            cur_v: 0,
            a: output.initial_a.clone(),
            b: vec![-1; input.bl],
            visited: vec![0],
            traffic_light: vec![0; input.n],
        }
    }

    fn copy_signals(&mut self, input: &Input, cs: CopySignals) -> Result<()> {
        self.op_cnt += 1;
        self.score += 1;

        // Copy from array A
        if cs.p_a + cs.len > input.al {
            bail!("R_A must be included in the array A.");
        }

        // Paste to array B
        if cs.p_b + cs.len > input.bl {
            bail!("R_B must be included in the array B.");
        }
        for i in 0..cs.len {
            if self.b[i + cs.p_b] >= 0 {
                self.traffic_light[self.b[i + cs.p_b] as usize] -= 1;
            }
        }
        for i in 0..cs.len {
            let val = self.a[i + cs.p_a];
            self.traffic_light[val] += 1;
            self.b[i + cs.p_b] = val as isize;
        }

        Ok(())
    }

    fn move_(&mut self, input: &Input, g: &Vec<HashSet<usize>>, v: usize, k: usize) -> Result<()> {
        self.op_cnt += 1;
        if self.traffic_light[v] == 0 {
            bail!(format!("Vertex {} must be green.", v));
        }

        if g[self.cur_v].contains(&v) {
            if self.target_idx < input.t_sz && input.t[self.target_idx] == v {
                self.target_idx += 1;
            }
            self.cur_v = v;
            self.visited.push(v);
            if self.visited.len() > k {
                self.visited.remove(0);
            }
        } else {
            bail!(format!(
                "You tried to move to vetex {} which is not adjacent to vertex {}.",
                v, self.cur_v
            ));
        }

        Ok(())
    }
}

#[allow(dead_code)]
#[derive(Clone)]
pub struct VisData {
    state: State,
    initial_comments: Option<Vec<String>>,
    commented_op: Option<CommentedOp>,
}

pub struct JudgeResult {
    pub score: usize,
}

pub fn judge(
    input_s: &str,
    output_s: &str,
    vis_data_vec: &mut Vec<VisData>,
    input_res: &mut Input,
    k: usize,
) -> Result<JudgeResult> {
    let input = parse_input(input_s).context("input")?;
    *input_res = input.clone();
    let output = parse_output(output_s, &input).context("output")?;

    let mut g = vec![HashSet::new(); input.n];
    for &(u, v) in input.edges.iter() {
        g[u].insert(v);
        g[v].insert(u);
    }

    let mut state = State::new(&input, &output);
    vis_data_vec.push(VisData {
        state: state.clone(),
        initial_comments: Some(output.initial_comments.clone()),
        commented_op: None,
    });

    for (i, commented_op) in output.clone().commented_ops.into_iter().enumerate() {
        if i >= MAX_OPERATION_CNT {
            bail!("You can perform operations up to 100000 times");
        }
        let res = match commented_op.op {
            Op::CopySignals(cs) => state.copy_signals(&input, cs),
            Op::Move(v) => state.move_(&input, &g, v, k),
        };
        res.context(format!("{}-th operation", i))?;

        vis_data_vec.push(VisData {
            state: state.clone(),
            initial_comments: None,
            commented_op: Some(commented_op),
        });
    }

    if state.target_idx != input.t_sz {
        bail!("You must visit all targets.");
    }
    Ok(JudgeResult { score: state.score })
}

pub fn gen(seed: u64, al: Option<usize>, bl: Option<usize>) -> Result<Input> {
    let mut rng = ChaCha20Rng::seed_from_u64(seed ^ 94);

    let al = if let Some(val) = al {
        val
    } else {
        rng.gen_range(AL_LB as u64..=AL_UB as u64) as usize
    };

    if !(AL_LB..=AL_UB).contains(&al) {
        Err(anyhow!("Out of range: {}", al).context("L_A"))?;
    }

    let bl = if let Some(val) = bl {
        val
    } else {
        let bl_sqrt = rng.gen_range(BL_LB_SQRT as f64..BL_UB_SQRT as f64);
        bl_sqrt.powf(2.0).floor() as usize
    };

    let bl_lb = BL_LB_SQRT.pow(2);
    let bl_ub = BL_UB_SQRT.pow(2);
    if !(bl_lb..bl_ub).contains(&bl) {
        Err(anyhow!("Out of range: {}", bl).context("L_B"))?;
    }

    let vertex_min_distance: i64 = rng.gen_range(20 as i64..=30 as i64);
    let edge_max_distance: i64 = rng.gen_range(80 as i64..=140 as i64);
    let edge_erasing_ratio: f64 = rng.gen_range(0.0..0.5);

    let mut res = construct_planar_graph(
        N,
        &mut rng,
        vertex_min_distance,
        edge_max_distance,
        edge_erasing_ratio,
    );
    while let None = res {
        res = construct_planar_graph(
            N,
            &mut rng,
            vertex_min_distance,
            edge_max_distance,
            edge_erasing_ratio,
        );
    }
    let (edges, coordinates) = res.unwrap();

    let mut t = vec![];
    let mut now = 0;
    for _ in 0..T_SZ {
        let nxt: usize = rng.gen_range(0..(N - 1) as u64) as usize;
        if nxt < now {
            t.push(nxt);
            now = nxt;
        } else {
            t.push(nxt + 1);
            now = nxt + 1;
        }
    }

    Ok(Input {
        n: N,
        m: edges.len(),
        t_sz: T_SZ,
        al,
        bl,
        edges,
        t,
        coordinates,
    })
}

pub fn draw_svg(vis_data: &VisData, input: &Input, k: usize) -> SVG {
    let mut doc = new_svg();
    doc = draw_graph(vis_data, input, doc);
    doc = draw_cur_v(vis_data, input, doc);
    doc = draw_target(vis_data, input, doc);
    doc = draw_visited_vertices(vis_data, input, doc, k);
    draw_tooltips(input, doc)
}

#[cfg(target_arch = "wasm32")]
#[wasm_bindgen]
pub fn generate(seed: u64, al: Option<usize>, bl: Option<usize>) -> Result<String, JsError> {
    let al = al.filter(|&al| al != 0);
    let bl = bl.filter(|&bl| bl != 0);
    Ok(gen(seed, al, bl).unwrap().to_string())
}

#[cfg(target_arch = "wasm32")]
#[wasm_bindgen(getter_with_clone)]
pub struct SolInfo {
    pub error: Option<String>,
    pub score: usize,
    pub max_turn: usize,
}

#[cfg(target_arch = "wasm32")]
#[derive(Clone)]
struct VisCache {
    error: Option<String>,
    vis_data_vec: Vec<VisData>,
    input: Input,
}

#[cfg(target_arch = "wasm32")]
static VIS_CACHE: Lazy<Mutex<Option<VisCache>>> = Lazy::new(|| Mutex::new(None));

#[cfg(target_arch = "wasm32")]
#[wasm_bindgen]
pub fn get_sol_info(input_s: &str, output_s: &str, k: usize) -> Result<SolInfo, JsError> {
    let mut vis_data_vec = vec![];
    let mut input_res = Input::new();
    let res = judge(input_s, output_s, &mut vis_data_vec, &mut input_res, k);

    match res {
        Ok(res) => {
            *VIS_CACHE.lock().unwrap() = Some(VisCache {
                error: None,
                vis_data_vec: vis_data_vec.clone(),
                input: input_res,
            });

            let sol_info = SolInfo {
                error: None,
                score: res.score,
                max_turn: vis_data_vec.len(),
            };
            Ok(sol_info)
        }
        Err(err) => {
            *VIS_CACHE.lock().unwrap() = Some(VisCache {
                error: Some(format!("{:#}", err)),
                vis_data_vec: vis_data_vec.clone(),
                input: input_res,
            });

            let sol_info = SolInfo {
                error: Some(format!("{:#}", err)),
                score: 0,
                max_turn: vis_data_vec.len(),
            };
            Ok(sol_info)
        }
    }
}

#[cfg(target_arch = "wasm32")]
#[wasm_bindgen(getter_with_clone)]
pub struct VisResult {
    pub svg: String,
    pub score: usize,
    pub t_sz: usize,
    pub target_idx: usize,
    pub target_v: Option<usize>,
    pub cur_v: usize,
    pub op: Option<String>,
    pub a: Vec<usize>,
    pub b: Vec<isize>,
    pub copy_signals: Option<CopySignals>,
    pub initial_comments: Vec<String>,
    pub comments: Vec<String>,
}

#[cfg(target_arch = "wasm32")]
#[wasm_bindgen]
pub fn visualize(_input: &str, _output: &str, t: usize, k: usize) -> Result<VisResult, JsError> {
    console_error_panic_hook::set_once();

    let VisCache {
        error,
        vis_data_vec,
        input,
    } = VIS_CACHE.lock().unwrap().clone().unwrap();

    if t <= vis_data_vec.len() {
        let vis_data = &vis_data_vec[t];

        let copy_signals =
            vis_data
                .commented_op
                .as_ref()
                .and_then(|commented_op| match commented_op.op {
                    Op::CopySignals(cs) => Some(cs),
                    Op::Move(_) => None,
                });

        let initial_comments = match &vis_data.initial_comments {
            Some(initial_comments) => initial_comments.clone(),
            None => vec![],
        };

        let comments = match &vis_data.commented_op {
            Some(commented_op) => commented_op.comments.clone(),
            None => vec![],
        };

        Ok(VisResult {
            svg: draw_svg(&vis_data, &input, k).to_string(),
            score: vis_data.state.score,
            t_sz: input.t_sz,
            target_idx: vis_data.state.target_idx,
            target_v: input.t.get(vis_data.state.target_idx).cloned(),
            cur_v: vis_data.state.cur_v,
            op: vis_data
                .commented_op
                .as_ref()
                .map(|commented_op| commented_op.op.to_string()),
            a: vis_data.state.a.clone(),
            b: vis_data.state.b.clone(),
            copy_signals,
            initial_comments,
            comments,
        })
    } else {
        Err(JsError::new(&format!("{:#}", error.unwrap())))
    }
}

struct UnionFind {
    par: Vec<i32>,
}

impl UnionFind {
    fn new(n: usize) -> Self {
        UnionFind { par: vec![-1; n] }
    }

    fn root(&mut self, x: usize) -> usize {
        if self.par[x] < 0 {
            x
        } else {
            self.par[x] = self.root(self.par[x] as usize) as i32;
            self.par[x] as usize
        }
    }

    fn unite(&mut self, x: usize, y: usize) {
        let mut x = self.root(x);
        let mut y = self.root(y);
        if x == y {
            return;
        }

        if self.par[x] < self.par[y] {
            std::mem::swap(&mut x, &mut y);
        }

        self.par[y] += self.par[x];
        self.par[x] = y as i32;
    }
}

fn distance2(p1: (i64, i64), p2: (i64, i64)) -> i64 {
    (p1.0 - p2.0).pow(2) + (p1.1 - p2.1).pow(2)
}

type Point = (i64, i64);
type Line = (Point, Point);

fn is_crossing(l1: &Line, l2: &Line) -> bool {
    let &((x0, y0), (x1, y1)) = l1;
    let &((x2, y2), (x3, y3)) = l2;

    let v0 = (x0 - x1) * (y2 - y0) + (y0 - y1) * (x0 - x2); // p0p1 x p0p2
    let v1 = (x0 - x1) * (y3 - y0) + (y0 - y1) * (x0 - x3); // p0p1 x p0p3
    let v2 = (x2 - x3) * (y0 - y2) + (y2 - y3) * (x2 - x0); // p2p3 x p2p0
    let v3 = (x2 - x3) * (y1 - y2) + (y2 - y3) * (x2 - x1); // p2p3 x p2p1

    if v0 == 0 && v1 == 0 {
        return (x0.min(x1) <= x2 && x2 <= x0.max(x1) && y0.min(y1) <= y2 && y2 <= y0.max(y1))
            || (x0.min(x1) <= x3 && x3 <= x0.max(x1) && y0.min(y1) <= y3 && y3 <= y0.max(y1))
            || (x2.min(x3) <= x0 && x0 <= x2.max(x3) && y2.min(y3) <= y0 && y0 <= y2.max(y3))
            || (x2.min(x3) <= x1 && x1 <= x2.max(x3) && y2.min(y3) <= y1 && y1 <= y2.max(y3));
    }

    if (y0, x0) == (y2, x2) || (y0, x0) == (y3, x3) || (y1, x1) == (y2, x2) || (y1, x1) == (y3, x3)
    {
        return false;
    }

    return v0 * v1 <= 0 && v2 * v3 <= 0;
}

fn check_connectivity(n: usize, edges: &Vec<(usize, usize)>, sel: &Vec<bool>) -> bool {
    let mut uf = UnionFind::new(n);
    for i in 0..sel.len() {
        if sel[i] {
            let (u, v) = edges[i];
            uf.unite(u, v);
        }
    }
    let r = uf.root(0);
    uf.par[r] == -(n as i32)
}

fn construct_planar_graph(
    n: usize,
    rng: &mut ChaCha20Rng,
    vertex_min_distance: i64,
    edge_max_distance: i64,
    edge_erasing_ratio: f64,
) -> Option<(Vec<(usize, usize)>, Vec<(i64, i64)>)> {
    let mut coords = vec![];
    let mut edges = vec![];
    let mut graph = vec![HashSet::new(); n];

    while coords.len() < n {
        let x = rng.gen_range(0i64..=1000);
        let y = rng.gen_range(0i64..=1000);
        if coords
            .iter()
            .any(|&p| distance2(p, (x, y)) < vertex_min_distance.pow(2))
        {
            continue;
        }
        coords.push((x, y));
    }

    let mut short_cands = vec![];
    let mut long_cands = vec![];
    for i in 0..n {
        for j in i + 1..n {
            if distance2(coords[i], coords[j]) > edge_max_distance.pow(2) {
                continue;
            }

            // The conditional means distance2(coords[i], coords[j]) <= (edge_max_distance / 2).pow(2)
            if 4 * distance2(coords[i], coords[j]) <= edge_max_distance.pow(2) {
                short_cands.push((i, j));
            } else {
                long_cands.push((i, j));
            }
        }
    }

    let mut cands_list = short_cands;
    cands_list.shuffle(rng);
    for (u, v) in cands_list {
        if edges
            .iter()
            .any(|&(e0, e1)| is_crossing(&(coords[u], coords[v]), &(coords[e0], coords[e1])))
        {
            continue;
        }
        edges.push((u, v));
        graph[u].insert(v);
        graph[v].insert(u);
    }

    cands_list = long_cands;
    cands_list.shuffle(rng);
    for (u, v) in cands_list {
        if edges
            .iter()
            .any(|&(e0, e1)| is_crossing(&(coords[u], coords[v]), &(coords[e0], coords[e1])))
        {
            continue;
        }
        edges.push((u, v));
        graph[u].insert(v);
        graph[v].insert(u);
    }

    edges.shuffle(rng);
    let mut sel = vec![true; edges.len()];
    let res = check_connectivity(n, &edges, &sel);
    if !res {
        return None;
    }
    for i in 0..edges.len() {
        sel[i] = false;
        let res = check_connectivity(n, &edges, &sel);
        if res {
            let val = rng.gen_range(0.0..1.0);
            if val > edge_erasing_ratio {
                sel[i] = true;
            }
        } else {
            sel[i] = true;
        }
    }

    let mut res_edges = vec![];
    for i in 0..edges.len() {
        if sel[i] {
            res_edges.push(edges[i]);
        }
    }

    Some((res_edges, coords))
}
